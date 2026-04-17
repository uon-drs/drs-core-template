namespace TemplateApp.Api.Auth;

using Microsoft.AspNetCore.Authorization;

public static class AuthPolicies
{
  public static AuthorizationPolicy IsAuthenticatedUser
    => new AuthorizationPolicyBuilder()
      .RequireAuthenticatedUser()
      .Build();

  public static AuthorizationPolicy CanCreateUsers
    => new AuthorizationPolicyBuilder()
      .Combine(IsAuthenticatedUser)
      .RequireClaim(ClaimTypes.Role, Roles.CreateUsers)
      .Build();

  public static AuthorizationPolicy CanUpdateUsers
    => new AuthorizationPolicyBuilder()
      .Combine(IsAuthenticatedUser)
      .RequireClaim(ClaimTypes.Role, Roles.UpdateUsers)
      .Build();

  public static AuthorizationPolicy CanDeleteUsers
    => new AuthorizationPolicyBuilder()
      .Combine(IsAuthenticatedUser)
      .RequireClaim(ClaimTypes.Role, Roles.DeleteUsers)
      .Build();

  public static AuthorizationPolicy CanViewUsers
    => new AuthorizationPolicyBuilder()
      .Combine(IsAuthenticatedUser)
      .RequireClaim(ClaimTypes.Role, Roles.ViewUsers)
      .Build();
}
