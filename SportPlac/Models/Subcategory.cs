namespace SportPlac.Models
{
    public class Subcategory
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Guid? ParentId { get; set; }
        public Subcategory? Parent { get; set; }
        public ICollection<Subcategory> Children { get; set; } = new List<Subcategory>();
        public Category Category { get; set; } = null!;
    }
}
