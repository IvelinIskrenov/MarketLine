namespace MarketLine.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public Category? Parent { get; set; }

        public List<Category> Children { get; set; } = new();

        public List<Item>? Items { get; set; }

        public Category() { }
    }
}
