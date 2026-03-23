namespace SportPlac.Models
{
    public class Subcategory
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public Category Category { get; set; } = null!;
    }
}
