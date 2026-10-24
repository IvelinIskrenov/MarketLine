using System.Net;
using System.Text.RegularExpressions;

namespace MarketLine.Tests
{
    public static class FormRequest
    {
        public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            return ReadToken(html);
        }

        public static string ReadToken(string html)
        {
            var match = Regex.Match(
                html,
                @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                match = Regex.Match(
                    html,
                    @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                    RegexOptions.IgnoreCase);
            }

            if (!match.Success)
            {
                throw new InvalidOperationException("Anti-forgery token was not found.");
            }

            return WebUtility.HtmlDecode(match.Groups[1].Value);
        }

        public static FormUrlEncodedContent Create(
            string antiForgeryToken,
            params (string Key, string Value)[] fields)
        {
            var pairs = fields
                .Select(field => new KeyValuePair<string, string>(field.Key, field.Value))
                .Append(new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken));

            return new FormUrlEncodedContent(pairs);
        }
    }
}
