namespace TLALOCSG.Services.Email;
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}
