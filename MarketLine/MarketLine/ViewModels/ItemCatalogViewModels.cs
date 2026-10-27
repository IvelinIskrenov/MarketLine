namespace MarketLine.Models
{
    public class ItemSearchQuery
    {
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Brand { get; set; }
        public string? Feature { get; set; }
        public string? Sort { get; set; }
    }

    public class ItemCatalogViewModel
    {
        public List<Item> Items { get; set; } = new();
        public ItemSearchQuery Query { get; set; } = new();
        public List<string> Brands { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }

    public class ItemSuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Brand { get; set; }
    }
}
