namespace MarketLine.Localization
{
    public class ShopText : IShopText
    {
        private readonly IHttpContextAccessor _http;

        public ShopText(IHttpContextAccessor http)
        {
            _http = http;
        }

        public bool IsEnglish => ShopCultures.IsEnglish(_http.HttpContext);

        public string Culture => IsEnglish ? ShopCultures.English : ShopCultures.Bulgarian;

        public string this[string key] => ShopTranslator.Ui(key, IsEnglish);

        public string Content(string? text) => ShopTranslator.Content(text, IsEnglish);

        public string Format(string key, params object[] args) => string.Format(this[key], args);
    }
}
