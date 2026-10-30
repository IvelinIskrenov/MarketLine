using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;

namespace MarketLine.Services
{
    public class ShopEmailSender : IEmailSender, IEmailInbox
    {
        private readonly ConcurrentQueue<ShopEmail> _messages = new();
        private readonly IConfiguration _config;
        private readonly ILogger<ShopEmailSender> _logger;

        public ShopEmailSender(IConfiguration config, ILogger<ShopEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public IReadOnlyList<ShopEmail> Messages => _messages.ToArray();

        public void Clear()
        {
            while (_messages.TryDequeue(out _))
            {
            }
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var email = new ShopEmail
            {
                To = to,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now
            };
            _messages.Enqueue(email);
            _logger.LogInformation("Email to {To}: {Subject}", to, subject);

            var host = _config["Email:SmtpHost"];
            if (string.IsNullOrWhiteSpace(host))
            {
                return;
            }

            try
            {
                var from = _config["Email:From"] ?? "noreply@marketline.bg";
                var port = _config.GetValue("Email:SmtpPort", 587);
                var user = _config["Email:SmtpUser"];
                var password = _config["Email:SmtpPassword"];
                var enableSsl = _config.GetValue("Email:SmtpEnableSsl", true);

                using var message = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = body
                };
                message.To.Add(to);

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl
                };
                if (!string.IsNullOrWhiteSpace(user))
                {
                    client.Credentials = new NetworkCredential(user, password);
                }

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed for {To}", to);
            }
        }
    }
}
