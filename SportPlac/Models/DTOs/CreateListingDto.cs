namespace SportPlac.Models.DTOs
{
    public class CreateListingDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }

        public Guid CategoryId { get; set; }
        public Guid? SubcategoryId { get; set; }

        public string Location { get; set; }
        public ItemCondition Condition { get; set; }
        public string? Brand { get; set; }

        public List<string>? Tags { get; set; }
        public List<IFormFile>? Images { get; set; } // max 8
    }

}
