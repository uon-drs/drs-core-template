namespace TemplateApp.Api.Tests.Fixtures;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Fake auth handler that reads claims from the <c>X-Test-Claims</c> request header.
/// </summary>
public class TestAuthHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  public const string SchemeName = "TestScheme";
  public const string ClaimsHeader = "X-Test-Claims";

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue(ClaimsHeader, out var headerValue))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    var model = JsonSerializer.Deserialize<ClaimModel[]>(headerValue.ToString()) ?? [];
    var claims = model.Select(x => new Claim(x.Type, x.Value));

    var identity = new ClaimsIdentity(claims, SchemeName);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, SchemeName);
    return Task.FromResult(AuthenticateResult.Success(ticket));
  }

  private record ClaimModel(string Type, string Value);
}
