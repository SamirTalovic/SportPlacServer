namespace SportPlac.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}
