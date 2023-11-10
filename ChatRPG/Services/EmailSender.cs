using System.Drawing.Printing;
using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace ChatRPG.Services;

public class EmailSender : IEmailSender
{

    private readonly string _senderEmail;
    private readonly string _senderPassword;

    public EmailSender(IConfiguration configuration)
    {
        _senderEmail = configuration.GetSection("ChatRPGEmail").GetValue<string>("Email");
        _senderPassword = configuration.GetSection("ChatRPGEmail").GetValue<string>("Password");
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        MimeMessage mail = new MimeMessage();
        mail.From.Add(new MailboxAddress("ChatRPG", _senderEmail));
        mail.To.Add(new MailboxAddress(email, email));
        mail.Subject = subject;
        mail.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlMessage
        };

        SmtpClient smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync("smtp.gmail.com", 0, true);
        await smtpClient.AuthenticateAsync(_senderEmail, _senderPassword);
        await smtpClient.SendAsync(mail);
        await smtpClient.DisconnectAsync(true);
    }
}
