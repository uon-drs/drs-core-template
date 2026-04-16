namespace TemplateApp.Api.Config;

public class KeycloakOptions
{
  /// <summary>
  /// Keycloak server.
  /// </summary>
  public string AuthServerUrl { get; init; } = "";

  /// <summary>
  /// Keycloak realm name.
  /// </summary>
  public string Realm { get; init; } = "";

  /// <summary>
  /// Client ID. Must match the <c>aud</c> claim in tokens issued by Keycloak.
  /// </summary>
  public string Resource { get; init; } = "";

  /// <summary>
  /// Client ID of the public client used for API docs and development testing.
  /// </summary>
  public string PublicClientId { get; init; } = "";

  /// <summary>
  /// Authority URL derived from <see cref="AuthServerUrl"/> and <see cref="Realm"/>.
  /// </summary>
  public string Authority => $"{AuthServerUrl.TrimEnd('/')}/realms/{Realm}";
}
