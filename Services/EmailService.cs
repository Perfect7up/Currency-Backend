using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}

public class EmailService(IConfiguration config) : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(config["Smtp:SenderName"], config["Smtp:SenderEmail"]));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlContent };
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        smtp.Timeout = 15000;

        try
        {
            await smtp.ConnectAsync(config["Smtp:Host"], int.Parse(config["Smtp:Port"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(config["Smtp:Username"], config["Smtp:Password"]);
            await smtp.SendAsync(email);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}