using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace TLALOCSG.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _cfg;
    public SmtpEmailSender(IOptions<SmtpSettings> options) => _cfg = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_cfg.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_cfg.Host, _cfg.Port,
            _cfg.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        if (!string.IsNullOrWhiteSpace(_cfg.User))
            await client.AuthenticateAsync(_cfg.User, _cfg.Password);

        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}
