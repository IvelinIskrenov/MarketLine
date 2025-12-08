namespace MarketLine.Models
{
    public class Item
    {
        public int id { get; set; }

        public string? name { get; set; }

        public string? description { get; set; }

        public double discount { get; set; }

        public double price { get; set; }

        public string? category { get; set; }

        public int stock_quantity { get; set; }

        public Item() { }
    }
}
