using System.ComponentModel.DataAnnotations;

namespace MarketLine.Security
{
    public sealed class HttpUrlAttribute : ValidationAttribute
    {
        public HttpUrlAttribute()
            : base("Въведете валиден http или https адрес.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return value is string url
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && url.Length <= InputLimits.Url;
        }
    }
}
