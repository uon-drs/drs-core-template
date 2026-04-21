namespace TemplateApp.Api.Models.Emails;

/// <summary>
/// Email check.
/// </summary>
/// <param name="CheckedAtUtc">Time stamp.</param>
public record HealthCheckEmailModel(DateTime CheckedAtUtc);
