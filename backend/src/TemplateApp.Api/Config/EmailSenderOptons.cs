namespace TemplateApp.Api.Config;

public record BaseEmailSenderOptions
{
  public BaseEmailSenderOptions()
  {
    if (string.IsNullOrWhiteSpace(ReplyToAddress))
    {
      ReplyToAddress = FromAddress;
    }
  }

  public string ServiceName { get; init; } = "TemplateApp";
  public string FromName { get; init; } = "No Reply";
  public string FromAddress { get; init; } = "noreply@example.com";
  public string ReplyToAddress { get; init; } = string.Empty;
  public List<string> ExcludedEmailAddresses { get; init; } = new List<string>();
}

public record LocalDiskEmailOptions : BaseEmailSenderOptions
{
  public string LocalPath { get; init; } = "~/temp";
}

public record SendGridOptions : BaseEmailSenderOptions
{
  public string SendGridApiKey { get; init; } = string.Empty;
}

public record SmtpOptions : BaseEmailSenderOptions
{
  public string SmtpHost { get; init; } = string.Empty;
  public int SmtpPort { get; init; }
  public int SmtpSecureSocketEnum { get; init; }

  public bool UseOAuth { get; init; }

  /// <example>
  /// Microsoft token endpoint: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
  /// </example>
  public string OAuthTokenEndpoint { get; init; } = string.Empty;

  /// <summary>
  /// Microsoft 365 scope: https://graph.microsoft.com/.default
  /// </summary>
  public string OAuthScope { get; init; } = "https://outlook.office365.com/.default";
  public string OAuthClientId { get; init; } = string.Empty;
  public string OAuthClientSecret { get; init; } = string.Empty;

  public string SmtpUsername { get; init; } = string.Empty;
  public string SmtpPassword { get; init; } = string.Empty;
}
