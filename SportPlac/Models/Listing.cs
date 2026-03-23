namespace SportPlac.Models
{
    public class Listing
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? SubcategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "RSD";
        public ItemCondition Condition { get; set; }
        public string? Brand { get; set; }
        public string Location { get; set; } = string.Empty;
        public ListingStatus Status { get; set; } = ListingStatus.Active;
        public bool IsPromoted { get; set; } = false;
        public int ViewsCount { get; set; } = 0;
        public int ReportsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RenewedAt { get; set; }

        // Navigation
        public User Seller { get; set; } = null!;
        public Guid SellerId { get; set; }
        public Category Category { get; set; } = null!;
        public Subcategory? Subcategory { get; set; }
        public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
        public ICollection<ListingTag> Tags { get; set; } = new List<ListingTag>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<ListingReport> Reports { get; set; } = new List<ListingReport>();
        public Store Store { get; set; }
        public Guid StoreId { get; set; }
    }

}
