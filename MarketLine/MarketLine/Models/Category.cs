namespace MarketLine.Models
{
    public class Category
    {
        /// <summary>
        /// Category of the Item
        /// </summary>
        public int Id { get; set; } 
        public string Name { get; set; }
        public List<Item>? Items{ get; set; }
        public Category() { }
    }
}
