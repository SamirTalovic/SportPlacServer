namespace SportPlac.Models
{
    public class Like
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? ListingId { get; set; }
        public Guid? StoreId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Listing? Listing { get; set; }
        public Store? Store { get; set; }
    }

}
