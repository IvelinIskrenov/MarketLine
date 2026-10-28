using MarketLine.Models;

namespace MarketLine.Data
{
    public static class ItemInventoryData
    {
        public static void EnsureVariants(Item item)
        {
            if (item.Variants.Count > 0)
            {
                SyncStock(item);
                return;
            }

            var name = item.name ?? string.Empty;
            if (name.Equals("Хидратиращ крем за лице", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "50 ml", Color = "Бял", Model = "Дневен", StockQuantity = 10 });
                item.Variants.Add(new ItemVariant { Size = "100 ml", Color = "Бял", Model = "Дневен", StockQuantity = 8 });
            }
            else if (name.Equals("Слънцезащитен флуид SPF 50", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "40 ml", Color = "Прозрачен", Model = "SPF 50", StockQuantity = 7 });
                item.Variants.Add(new ItemVariant { Size = "80 ml", Color = "Прозрачен", Model = "SPF 50", StockQuantity = 5 });
            }
            else if (name.Equals("Лосион за тяло с ший", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "200 ml", Color = "Бежов", Model = "Подхранващ", StockQuantity = 15 });
                item.Variants.Add(new ItemVariant { Size = "400 ml", Color = "Бежов", Model = "Подхранващ", StockQuantity = 10 });
            }
            else if (name.Equals("Душ гел с алое", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "250 ml", Color = "Зелен", Model = "Алое", StockQuantity = 25 });
                item.Variants.Add(new ItemVariant { Size = "500 ml", Color = "Зелен", Model = "Алое", StockQuantity = 15 });
            }
            else if (name.Equals("Витамин C 1000 mg", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "30 табл.", Model = "Имунитет", StockQuantity = 30 });
                item.Variants.Add(new ItemVariant { Size = "60 табл.", Model = "Имунитет", StockQuantity = 20 });
            }
            else if (name.Equals("Омега-3 капсули", StringComparison.OrdinalIgnoreCase))
            {
                item.Variants.Add(new ItemVariant { Size = "60 капс.", Model = "EPA/DHA", StockQuantity = 33 });
            }
            else
            {
                item.Variants.Add(new ItemVariant
                {
                    Model = InventoryRules.DefaultLabel,
                    StockQuantity = Math.Max(0, item.stock_quantity)
                });
            }

            SyncStock(item);
        }

        public static void SyncStock(Item item)
        {
            item.stock_quantity = item.Variants.Sum(variant => variant.StockQuantity);
        }

        public static ItemVariant? Resolve(Item item, int? variantId)
        {
            if (item.Variants.Count == 0)
            {
                return null;
            }

            if (variantId is > 0)
            {
                return item.Variants.FirstOrDefault(variant => variant.Id == variantId.Value);
            }

            return item.Variants.Count == 1
                ? item.Variants[0]
                : item.Variants.FirstOrDefault(variant => variant.StockQuantity > 0) ?? item.Variants[0];
        }
    }
}
