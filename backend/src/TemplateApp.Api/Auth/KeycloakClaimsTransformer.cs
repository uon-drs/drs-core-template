namespace TemplateApp.Api.Auth;

using System.Security.Claims;
using System.Text.Json;
using Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// Maps Keycloak role claims from the JWT into the standard role claim type expected by
/// <see cref="AuthPolicies" />. Keycloak encodes roles in two nested JSON structures:
/// <list type="bullet">
///   <item><c>realm_access.roles</c> — realm-level roles</item>
///   <item><c>resource_access.{clientId}.roles</c> — client-level roles</item>
/// </list>
/// Both are added as <c>Role</c> claims so that <c>RequireClaim</c> policies resolve correctly.
/// </summary>
public class KeycloakClaimsTransformer : IClaimsTransformation
{

  private readonly string _clientId;

  public KeycloakClaimsTransformer(IOptions<KeycloakOptions> options) => _clientId = options.Value.Resource;

  /// <inheritdoc />
  public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
    {
      return Task.FromResult(principal);
    }

    var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    ExtractRealmRoles(principal, roles);
    ExtractClientRoles(principal, roles);

    foreach (var role in roles.Where(role => !identity.HasClaim(ClaimTypes.Role, role)))
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    return Task.FromResult(principal);
  }

  private static void ExtractRealmRoles(ClaimsPrincipal principal, HashSet<string> roles)
  {
    var realmAccess = principal.FindFirstValue(KeycloakClaims.RealmAccess);
    if (string.IsNullOrEmpty(realmAccess))
    {
      return;
    }

    ParseRolesFromJson(realmAccess, roles);
  }

  private void ExtractClientRoles(ClaimsPrincipal principal, HashSet<string> roles)
  {
    var resourceAccess = principal.FindFirstValue(KeycloakClaims.ResourceAccess);
    if (string.IsNullOrEmpty(resourceAccess))
    {
      return;
    }

    try
    {
      using var doc = JsonDocument.Parse(resourceAccess);
      if (!doc.RootElement.TryGetProperty(_clientId, out var clientElement))
      {
        return;
      }

      ParseRolesFromElement(clientElement, roles);
    }
    catch (JsonException)
    {
    }
  }

  private static void ParseRolesFromJson(string json, HashSet<string> roles)
  {
    try
    {
      using var doc = JsonDocument.Parse(json);
      ParseRolesFromElement(doc.RootElement, roles);
    }
    catch (JsonException)
    {
    }
  }

  private static void ParseRolesFromElement(JsonElement element, HashSet<string> roles)
  {
    if (!element.TryGetProperty(KeycloakClaims.Roles, out var rolesArray))
    {
      return;
    }

    foreach (var role in rolesArray.EnumerateArray())
    {
      var value = role.GetString();
      if (!string.IsNullOrEmpty(value))
      {
        roles.Add(value);
      }
    }
  }
}
