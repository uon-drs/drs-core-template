using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using TemplateApp.Api.Config;
using TemplateApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Azure Key Vault configuration
// When running in Azure the App Service Managed Identity is used automatically.
// Locally, DefaultAzureCredential falls back to developer credentials (az login, VS, etc.)
// ============================================================
var keyVaultName = builder.Configuration["KeyVaultName"];
if (!string.IsNullOrEmpty(keyVaultName))
{
  builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
}

// ============================================================
// Database — PostgreSQL via EF Core
// Connection string is stored in Azure Key Vault and referenced
// in App Service settings as a Key Vault reference.
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// Authentication — Keycloak JWT Bearer
// The backend validates tokens issued by Keycloak without
// needing to exchange them. The frontend passes its Keycloak
// access token directly in the Authorization header.
// ============================================================
var keycloak = builder.Configuration.GetSection("Keycloak").Get<KeycloakOptions>()
               ?? throw new InvalidOperationException("Keycloak configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    // Keycloak OIDC discovery document is at {Authority}/.well-known/openid-configuration
    options.Authority = keycloak.Authority;
    options.Audience = keycloak.Resource;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ClockSkew = TimeSpan.FromSeconds(30),
    };
  });

builder.Services.AddAuthorization();

// ============================================================
// CORS — allow the frontend origin
// Origins are configured per-environment via app settings.
// ============================================================
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

builder.Services.AddControllers();

// ============================================================
// OpenAPI — only in Development; Scalar UI with Keycloak PKCE flow
// ============================================================
if (builder.Environment.IsDevelopment())
{
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen(o =>
  {
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    o.IncludeXmlComments(xmlPath);

    o.SwaggerDoc("v1", new OpenApiInfo
    {
      Title = "TemplateApp API", Version = "v1"
    });

    const string oauthSchemeId = "oauth2";
    o.AddSecurityDefinition(oauthSchemeId, new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.OAuth2,
      Flows = new OpenApiOAuthFlows
      {
        AuthorizationCode = new OpenApiOAuthFlow
        {
          AuthorizationUrl = new Uri($"{keycloak.Authority}/protocol/openid-connect/auth"),
          TokenUrl = new Uri($"{keycloak.Authority}/protocol/openid-connect/token"),
          Scopes = new Dictionary<string, string>
          {
            {
              "openid", "OpenID Connect"
            },
            {
              "profile", "User profile"
            },
            {
              "email", "User email"
            },
          }
        }
      }
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
      {
        new OpenApiSecurityScheme
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme, Id = oauthSchemeId
          }
        },
        ["openid", "profile"]
      }
    });
  });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
  app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program
{
}
