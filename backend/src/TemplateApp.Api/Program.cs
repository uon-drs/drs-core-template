using Azure.Identity;
using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk.Kiota;
using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TemplateApp.Api.Auth;
using TemplateApp.Api.Config;
using TemplateApp.Api.Constants;
using TemplateApp.Api.Data;
using TemplateApp.Api.Data.Seeder;
using TemplateApp.Api.Extensions;
using TemplateApp.Api.Services;

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
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));

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

builder.Services.AddAuthorization(AuthConfiguration.AuthOptions);
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformer>();

// ============================================================
// Keycloak admin client — machine-to-machine realm management
// Uses client credentials flow; token lifecycle managed by Duende.
// ============================================================
var keycloakAdminClientOptions = new KeycloakAdminClientOptions
{
  AuthServerUrl = keycloak.AuthServerUrl,
  Realm = keycloak.Realm,
  Resource = keycloak.Resource,
  Credentials = new KeycloakClientInstallationCredentials
  {
    Secret = keycloak.Secret
  }
};

builder.Services
  .AddKeycloakAdminHttpClient(keycloakAdminClientOptions)
  .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse(keycloakAdminClientOptions.Resource));

builder.Services.AddDistributedMemoryCache();
builder.Services
  .AddClientCredentialsTokenManagement()
  .AddClient(
    ClientCredentialsClientName.Parse(keycloakAdminClientOptions.Resource),
    o =>
    {
      o.ClientId = ClientId.Parse(keycloakAdminClientOptions.Resource);
      o.ClientSecret = ClientSecret.Parse(keycloakAdminClientOptions.Credentials.Secret);
      o.TokenEndpoint = new Uri(keycloakAdminClientOptions.KeycloakTokenEndpoint);
    }
  );

// ============================================================
// CORS — allow the frontend origin
// Origins are configured per-environment via app settings.
// ============================================================
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o =>
  o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials()));

// ============================================================
// App services
// ============================================================
builder.Services
  .AddApplicationInsightsTelemetry()
  .AddControllersWithViews()
  .AddJsonOptions(DefaultJsonOptions.Configure);

builder.Services
  .AddEmailSender(builder.Configuration)
  .AddScoped<UserService>();

// ============================================================
// OpenAPI — Development only; Scalar UI with Keycloak PKCE flow
// XML doc comments are picked up automatically from the generated XML file.
// ============================================================
const string oAuth2SchemeIdentifier = "oAuth2";

if (builder.Environment.IsDevelopment())
{
  builder.Services.AddOpenApi("v1", o =>
  {
    o.AddDocumentTransformer((document, _, _) =>
    {
      document.Info = new OpenApiInfo
      {
        Title = "TemplateApp API", Version = "v1"
      };

      var oAuth2Scheme = new OpenApiSecurityScheme
      {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
          AuthorizationCode = new OpenApiOAuthFlow
          {
            AuthorizationUrl = new Uri($"{keycloak.Authority}/protocol/openid-connect/auth"),
            TokenUrl = new Uri($"{keycloak.Authority}/protocol/openid-connect/token"),
          }
        }
      };
      document.Components ??= new OpenApiComponents();
      document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
      document.Components.SecuritySchemes.Add(oAuth2SchemeIdentifier, oAuth2Scheme);
      return Task.CompletedTask;
    });

    o.AddOperationTransformer((operation, _, _) =>
    {
      operation.Security =
      [
        new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecuritySchemeReference(oAuth2SchemeIdentifier), []
          }
        }
      ];
      return Task.CompletedTask;
    });
  });
}

var app = builder.Build();

// ============================================================
// Data seeding, runs at the startup
// ============================================================
using var scope = app.Services.CreateScope();
var keycloakClient = scope.ServiceProvider.GetRequiredService<KeycloakAdminApiClient>();
var keycloakOptions = scope.ServiceProvider.GetRequiredService<IOptions<KeycloakOptions>>();
var keycloakDataSeeder = new KeycloakDataSeeder(keycloakClient, keycloakOptions);
await keycloakDataSeeder.SeedKeycloakData();

// ============================================================
// Middleware pipeline
// ============================================================
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference("/api-docs", o =>
  {
    o.AddDocument("v1", "/openapi/v1.json");
    o.AddPreferredSecuritySchemes([oAuth2SchemeIdentifier]);
    o.AddAuthorizationCodeFlow(oAuth2SchemeIdentifier, flow =>
      flow
        .WithClientId(keycloak.PublicClientId)
        .WithPkce(Pkce.Sha256)
    );
  });
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
