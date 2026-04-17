namespace TemplateApp.Api.Models.User;

using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;

/// <summary>
/// Represents a user.
/// </summary>
/// <param name="Id">User ID.</param>
/// <param name="Firstname">First name.</param>
/// <param name="Lastname">Last name.</param>
/// <param name="Email">Email address.</param>
/// <param name="Groups">List of groups the user belongs to.</param>
/// <param name="IsEnabled">Whether the user is enabled.</param>
public record User(
  string Id,
  string? Firstname,
  string? Lastname,
  string? Email,
  List<string> Groups,
  bool IsEnabled
)
{
  public User(UserRepresentation user, IEnumerable<GroupRepresentation>? groups)
    : this(
      user.Id!,
      user.FirstName,
      user.LastName,
      user.Email,
      groups?.Select(x => x.Name ?? string.Empty).ToList() ?? [],
      user.Enabled ?? false
    )
  {
  }
}

public record PartialUser(string? Firstname, string? Lastname, string? Email, bool IsEnabled);
public record Group(string Id, string Name);
