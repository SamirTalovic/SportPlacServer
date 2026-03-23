namespace SportPlac.Models
{
    public class Review
    {
        public Guid Id { get; set; }
        public Guid ReviewerId { get; set; }
        public Guid SellerId { get; set; }
        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Reviewer { get; set; } = null!;
        public User Seller { get; set; } = null!;
        public Store Store { get; set; }
        public Guid StoreId { get; set; }
    }
}
