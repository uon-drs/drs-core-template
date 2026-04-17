namespace TemplateApp.Api.Services.EmailSender;

using Config;
using Contracts;
using EmailServices;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Models.Emails;

public class LocalDiskEmailSender : IEmailSender
{
  private readonly LocalDiskEmailOptions _config;
  private readonly RazorViewService _emailViews;
  private readonly string _localPath;

  public LocalDiskEmailSender(IOptions<LocalDiskEmailOptions> options, RazorViewService emailViews)
  {
    _config = options.Value;
    _emailViews = emailViews;

    // local path preprocessing
    // special case replacements, like `~`
    _localPath = _config.LocalPath.StartsWith("~/", StringComparison.Ordinal)
      ? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        _config.LocalPath.Replace("~/", ""))
      : _config.LocalPath;
  }

  /// <inheritdoc />
  public async Task SendEmail<TModel>(
    List<EmailAddress> toAddresses,
    string viewName,
    TModel model,
    List<EmailAddress>? ccAddresses = null
  ) where TModel : class
  {
    EnsureTargetPath();

    var (body, viewContext) = await _emailViews.RenderToString(viewName, model);

    var message = new MimeMessage();

    foreach (var address in toAddresses)
    {
      message.To.Add(!string.IsNullOrEmpty(address.Name)
        ? new MailboxAddress(address.Name, address.Address)
        : MailboxAddress.Parse(address.Address));
    }

    message.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));
    message.ReplyTo.Add(MailboxAddress.Parse(_config.ReplyToAddress));
    message.Subject = (string?)viewContext.ViewBag.Subject ?? string.Empty;

    message.Body = new TextPart(TextFormat.Html)
    {
      Text = body
    };

    if (ccAddresses != null)
    {
      foreach (var address in ccAddresses)
      {
        message.Cc.Add(!string.IsNullOrEmpty(address.Name)
          ? new MailboxAddress(address.Name, address.Address)
          : MailboxAddress.Parse(address.Address));
      }
    }

    await message.WriteToAsync(Path.Combine(_localPath, MessageFileName(viewName, toAddresses[0].Address)));
  }

  public async Task SendEmail<TModel>(
    EmailAddress toAddress,
    string viewName,
    TModel model,
    EmailAddress? ccAddress = null
  ) where TModel : class
  {
    var ccAddresses = new List<EmailAddress>();
    if (ccAddress != null)
    {
      ccAddresses.Add(ccAddress);
    }
    await SendEmail(new List<EmailAddress>
    {
      toAddress
    }, viewName, model, ccAddresses);
  }

  private void EnsureTargetPath()
  {
    if (!Directory.Exists(_localPath))
    {
      Directory.CreateDirectory(_localPath);
    }
  }

  private static string ShortViewName(string viewName) => viewName[(viewName.LastIndexOf('/') + 1)..];

  private static string SafeIsoDate(DateTimeOffset date) => date.ToString("o").Replace(":", "-");

  private static string MessageFileName(string viewName, string recipient)
    => $"{ShortViewName(viewName)}_{recipient}_{SafeIsoDate(DateTimeOffset.UtcNow)}.eml";
}
