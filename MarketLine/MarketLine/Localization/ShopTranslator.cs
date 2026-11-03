namespace MarketLine.Localization
{
    public static class ShopTranslator
    {
        private static readonly Lazy<List<KeyValuePair<string, string>>> LongestFirst = new(() =>
            ShopLanguage.Phrases
                .OrderByDescending(pair => pair.Key.Length)
                .ToList());

        public static string Ui(string key, bool english)
        {
            if (!ShopLanguage.Ui.TryGetValue(key, out var bulgarian))
            {
                return key;
            }

            return english ? Content(bulgarian, true) : bulgarian;
        }

        public static string Content(string? text, bool english)
        {
            if (string.IsNullOrWhiteSpace(text) || !english)
            {
                return text ?? string.Empty;
            }

            var trimmed = text.Trim();
            if (ShopLanguage.Phrases.TryGetValue(trimmed, out var exact))
            {
                return exact;
            }

            var translated = text;
            foreach (var pair in LongestFirst.Value)
            {
                if (pair.Key.Length == 0 || translated.IndexOf(pair.Key, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                translated = translated.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            }

            return translated;
        }

        public static List<string> SearchAliases(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return [];
            }

            var term = searchTerm.Trim();
            var aliases = new List<string> { term.ToLower() };
            foreach (var pair in ShopLanguage.Phrases)
            {
                if (pair.Key.Length < 4)
                {
                    continue;
                }

                if (pair.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    aliases.Add(pair.Key.ToLower());
                }
            }

            return aliases.Distinct().Take(25).ToList();
        }
    }
}
