using CRM.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CRM.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailSendResult> SendAsync(
            string to,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured())
            {
                _logger.LogWarning("SMTP send skipped: Smtp settings are incomplete.");
                return EmailSendResult.Fail("Email is not configured. Contact your administrator.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

                using var client = new SmtpClient();
                var secureSocketOptions = _options.Port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                return EmailSendResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed for recipient {Recipient}", to);
                return EmailSendResult.Fail("Unable to send email. Please try again later.");
            }
        }

        private bool IsConfigured() =>
            !string.IsNullOrWhiteSpace(_options.Host)
            && _options.Port > 0
            && !string.IsNullOrWhiteSpace(_options.FromEmail);
    }
}
