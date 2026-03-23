namespace SportPlac.Models
{
    public class ListingImage
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? OptimizedUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; } = false;
        public string? SeoFileName { get; set; } // Auto Image Renamer

        public Listing Listing { get; set; } = null!;
    }
}
