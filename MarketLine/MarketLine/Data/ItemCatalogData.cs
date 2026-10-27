using MarketLine.Models;

namespace MarketLine.Data
{
    public static class ItemCatalogData
    {
        public static void ApplyDefaults(Item item)
        {
            var name = item.name ?? string.Empty;

            if (name.Equals("Хидратиращ крем за лице", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "DermaLine", "Хидратация, За всеки тип кожа, Дневен", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            if (name.Equals("Слънцезащитен флуид SPF 50", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "SunCare", "SPF 50, Неомазняващ, За лице", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            if (name.Equals("Лосион за тяло с ший", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "BodySoft", "Подхранване, Суха кожа, За тяло", new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            if (name.Equals("Душ гел с алое", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "AloePure", "Натурално, За тяло, Ежедневна грижа", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            if (name.Equals("Витамин C 1000 mg", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "VitaPlus", "Имунитет, Таблетки, Хранителна добавка", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            if (name.Equals("Омега-3 капсули", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(item, "VitaPlus", "Сърце, Капсули, Хранителна добавка", new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
                return;
            }

            SetIfEmpty(item, "MarketLine", "Ежедневна грижа", DateTime.UtcNow);
        }

        private static void SetIfEmpty(Item item, string brand, string features, DateTime createdAt)
        {
            if (string.IsNullOrWhiteSpace(item.Brand))
            {
                item.Brand = brand;
            }

            if (string.IsNullOrWhiteSpace(item.Features))
            {
                item.Features = features;
            }

            if (item.CreatedAt == default)
            {
                item.CreatedAt = createdAt;
            }
        }
    }
}
