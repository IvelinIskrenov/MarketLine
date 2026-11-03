namespace MarketLine.Localization
{
    public interface IShopText
    {
        bool IsEnglish { get; }
        string Culture { get; }
        string this[string key] { get; }
        string Content(string? text);
        string Format(string key, params object[] args);
    }
}
