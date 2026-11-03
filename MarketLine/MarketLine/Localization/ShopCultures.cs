using Microsoft.AspNetCore.Localization;

namespace MarketLine.Localization
{
    public static class ShopCultures
    {
        public const string Bulgarian = "bg";
        public const string English = "en";
        public const string FormatCulture = "en-US";
        public const string CookieName = ".MarketLine.Culture";

        public static readonly string[] Supported = { Bulgarian, English };

        public static bool IsEnglish(HttpContext? http)
        {
            var ui = http?.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.TwoLetterISOLanguageName;
            if (!string.IsNullOrWhiteSpace(ui))
            {
                return string.Equals(ui, English, StringComparison.OrdinalIgnoreCase);
            }

            var cookie = http?.Request.Cookies[CookieName];
            return cookie != null
                && (cookie.Contains("uic=en", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cookie, English, StringComparison.OrdinalIgnoreCase));
        }

        public static void SetCookie(HttpResponse response, string culture)
        {
            var ui = Normalize(culture);
            response.Cookies.Append(
                CookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(FormatCulture, ui)),
                new CookieOptions
                {
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });
        }

        public static string Normalize(string? culture)
        {
            return string.Equals(culture, English, StringComparison.OrdinalIgnoreCase) ? English : Bulgarian;
        }
    }
}
