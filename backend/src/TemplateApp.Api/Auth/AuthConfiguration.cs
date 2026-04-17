namespace TemplateApp.Api.Auth;

using Microsoft.AspNetCore.Authorization;

public static class AuthConfiguration
{
  public static readonly Action<AuthorizationOptions> AuthOptions =
    b =>
    {
      // This is used when `[Authorize]` is provided with no specific policy / config
      b.DefaultPolicy = AuthPolicies.IsAuthenticatedUser;

      b.AddPolicy(nameof(AuthPolicies.CanViewUsers), AuthPolicies.CanViewUsers);
      b.AddPolicy(nameof(AuthPolicies.CanCreateUsers), AuthPolicies.CanCreateUsers);
      b.AddPolicy(nameof(AuthPolicies.CanUpdateUsers), AuthPolicies.CanUpdateUsers);
      b.AddPolicy(nameof(AuthPolicies.CanDeleteUsers), AuthPolicies.CanDeleteUsers);
      b.AddPolicy(nameof(AuthPolicies.CanSendHealthCheckEmail), AuthPolicies.CanSendHealthCheckEmail);
    };
}
