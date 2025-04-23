using System.Net.Mail;
using System.Net;
using IqraCore.Entities.Configuration;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Mail
{
    public class EmailManager
    {
        private readonly ILogger<EmailManager> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailManager(ILogger<EmailManager> logger, EmailSettings emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = true;

                    var message = new MailMessage(
                        from: new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                        to: new MailAddress(toEmail)
                    );
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    await client.SendMailAsync(message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with Subject {Subject}", toEmail, subject);
            }

            return false;
        }
    }
}
