namespace TemplateApp.Api.Auth;

/// <summary>
/// Keycloak JWT claim names.
/// </summary>
public static class KeycloakClaims
{
  /// <summary>
  /// Realm-level roles claim.
  /// </summary>
  public const string RealmAccess = "realm_access";

  /// <summary>
  /// Client-level roles claim, keyed by client ID.
  /// </summary>
  public const string ResourceAccess = "resource_access";

  /// <summary>
  /// Roles array key within <see cref="RealmAccess"/> and <see cref="ResourceAccess"/> objects.
  /// </summary>
  public const string Roles = "roles";
}

/// <summary>
/// Keycloak groups.
/// </summary>
public static class Groups
{
  public const string Admin = "Admin";
  public const string Guest = "Guest";
}

/// <summary>
/// Keycloak roles.
/// </summary>
public static class Roles
{
  public const string CreateUsers = "CreateUsers";
  public const string UpdateUsers = "UpdateUsers";
  public const string DeleteUsers = "DeleteUsers";
  public const string ViewUsers = "ViewUsers";

  public const string ViewContent = "ViewContent";
}
