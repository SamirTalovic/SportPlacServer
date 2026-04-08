namespace SportPlac.Models.DTOs
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int SortOrder { get; set; }

        public List<SubcategoryDto> Subcategories { get; set; }
    }

    public class SubcategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
    public class SubcategoryTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<SubcategoryTreeDto> Children { get; set; } = new();
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateSubcategoryDto
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
    }

}
