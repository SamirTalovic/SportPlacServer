    namespace SportPlac.Models
    {
        public class Store
        {
            public Guid Id { get; set; }
            public Guid UserId { get; set; }
            public string Name { get; set; }
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? AvatarUrl { get; set; }
            public bool IsVerified { get; set; }
            public int TotalSales { get; set; }
            public double ResponseRate { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public User User { get; set; }
            public ICollection<Listing> Listings { get; set; } = new List<Listing>();
            public ICollection<Review> Reviews { get; set; } = new List<Review>();
            public ICollection<Like> Likes { get; set; } = new List<Like>();
        }

    }
