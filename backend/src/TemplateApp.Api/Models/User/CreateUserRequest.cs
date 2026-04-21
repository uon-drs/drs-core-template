namespace TemplateApp.Api.Models.User;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Model for creating a user.
/// </summary>
/// <param name="Email">User email address.</param>
/// <param name="Groups">List of groups to assign the user to.</param>
/// <param name="IsEnabled">Whether the user is enabled.</param>
public record CreateUserRequest(
  [Required(ErrorMessage = "Email required.")] [EmailAddress(ErrorMessage = "Valid email required.")]
  string Email,
  [MinLength(1, ErrorMessage = "Must be in at least one group.")] List<string> Groups,
  bool IsEnabled
);
