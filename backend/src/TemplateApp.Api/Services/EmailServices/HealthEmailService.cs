namespace TemplateApp.Api.Services.EmailServices;

using Contracts;
using Models.Emails;

/// <summary>
/// Sends health check email.
/// </summary>
public class HealthEmailService
{
  private readonly IEmailSender _emails;

  public HealthEmailService(IEmailSender emails) => _emails = emails;

  public async Task SendHealthCheckEmail(EmailAddress to)
    => await _emails.SendEmail(to, "Emails/HealthCheck", new HealthCheckEmailModel(DateTime.UtcNow));
}
