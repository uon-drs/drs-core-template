namespace TemplateApp.Api.Extensions;

using Config;
using Services.Contracts;
using Services.EmailSender;
using Services.EmailServices;

public static class ServiceCollectionExtensions
{

  public static IServiceCollection AddEmailSender(this IServiceCollection s, IConfiguration c)
  {

    var emailProvider = c["OutboundEmail:Provider"] ?? string.Empty;

    var outboundProvider = emailProvider.ToLowerInvariant();

    switch (outboundProvider)
    {
      case "sendgrid":
        s.Configure<SendGridOptions>(c.GetSection("OutboundEmail"));
        s.AddTransient<IEmailSender, SendGridEmailSender>();
        break;

      case "smtp":
        s.Configure<SmtpOptions>(c.GetSection("OutboundEmail"));
        s.AddTransient<IEmailSender, SmtpEmailSender>();
        break;

      default:
        s.Configure<LocalDiskEmailOptions>(c.GetSection("OutboundEmail"));
        s.AddTransient<IEmailSender, LocalDiskEmailSender>();
        break;
    }

    s
      .AddTransient<RazorViewService>()
      .AddTransient<HealthEmailService>();

    return s;
  }
}
