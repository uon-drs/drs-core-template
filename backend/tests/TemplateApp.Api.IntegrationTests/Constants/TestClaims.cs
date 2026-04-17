namespace TemplateApp.Api.Tests.Constants;

using System.Security.Claims;
using Auth;
using AuthClaimTypes = Auth.ClaimTypes;
using SystemClaimTypes = System.Security.Claims.ClaimTypes;

/// <summary>
/// Claims for tests.
/// </summary>
internal static class TestClaims
{
  public static readonly Claim Name = new Claim(SystemClaimTypes.Name, "test-user");
  public static readonly Claim Email = new Claim(SystemClaimTypes.Email, "test@example.com");
  public static readonly Claim ViewUsersRole = new Claim(AuthClaimTypes.Role, Roles.ViewUsers);
}
