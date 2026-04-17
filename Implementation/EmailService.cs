using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly SmtpOptions _smtpOptions;

        public EmailService(IOptions<SmtpOptions> smtpOptions)
        {
            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Skip sending if SMTP credentials are still placeholders (e.g. in development)
            if (_smtpOptions.Username == "{SMTP_USER}" || string.IsNullOrEmpty(_smtpOptions.Username))
            {
                // In a real app, you might log this or handle it differently
                return;
            }

            var smtpClient = new SmtpClient(_smtpOptions.ServerAddress, _smtpOptions.ServerPort)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password),
                EnableSsl = true
            };

            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpOptions.Username),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (FormatException)
            {
                // Log or handle invalid email format
            }
        }
    }
}
