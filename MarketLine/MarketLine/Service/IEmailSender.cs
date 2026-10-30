namespace MarketLine.Services
{
    public class ShopEmail
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    public interface IEmailInbox
    {
        IReadOnlyList<ShopEmail> Messages { get; }
        void Clear();
    }
}
