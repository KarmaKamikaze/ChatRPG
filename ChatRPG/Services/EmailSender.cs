﻿using System.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace ChatRPG.Services;

public class EmailSender : IEmailSender
{
    private readonly string? _senderEmail;
    private readonly string? _senderPassword;
    private readonly ILogger<EmailSender> _logger;
    public readonly bool IsActive;
    

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _logger = logger;
        IsActive = configuration.GetSection("EmailServiceInfo").GetValue<bool>("ShouldSend");
        if (IsActive)
        {
            _senderEmail = configuration.GetSection("EmailServiceInfo").GetValue<string>("Email");
            _senderPassword = configuration.GetSection("EmailServiceInfo").GetValue<string>("Password");

            if (_senderEmail == null || _senderPassword == null)
            {
                throw new ConfigurationErrorsException("Missing 'Email' credentials in appsettings");
            }
        }
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

        try
        {
            SmtpClient smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync("smtp.gmail.com", 0, true);
            await smtpClient.AuthenticateAsync(_senderEmail, _senderPassword);
            await smtpClient.SendAsync(mail);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send email");
        }
    }
}
