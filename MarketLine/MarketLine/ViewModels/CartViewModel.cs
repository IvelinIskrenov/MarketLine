namespace MarketLine.Models
{
    public class CartLine
    {
        public int ItemId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartLine> Lines { get; set; } = new();
        public string? PromoCode { get; set; }
        public decimal SubTotal { get; set; }
        public decimal PromoDiscount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public decimal FreeShippingThreshold { get; set; }
        public decimal RemainingForFreeShipping { get; set; }
        public int ItemCount => Lines.Sum(line => line.Quantity);
    }
}
