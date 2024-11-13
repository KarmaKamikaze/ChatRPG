using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MimeKit.Text;

namespace ChatRPG.Services;

public class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly string? _senderEmail = configuration.GetSection("EmailServiceInfo").GetValue<string>("Email");

    private readonly string? _senderPassword =
        configuration.GetSection("EmailServiceInfo").GetValue<string>("Password");

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (_senderEmail == null || _senderPassword == null)
        {
            logger.LogError("Missing 'Email' credentials in appsettings");
            return;
        }

        var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress("ChatRPG", _senderEmail));
        mail.To.Add(new MailboxAddress(email, email));
        mail.Subject = subject;
        mail.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlMessage
        };

        try
        {
            var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync("smtp.gmail.com", 0, true);
            await smtpClient.AuthenticateAsync(_senderEmail, _senderPassword);
            await smtpClient.SendAsync(mail);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send email");
        }
    }
}
