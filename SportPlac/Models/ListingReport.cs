namespace SportPlac.Models
{
    public class ListingReport
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Listing Listing { get; set; } = null!;
        public User Reporter { get; set; } = null!;
        public Guid ReporterId { get; set; }
    }
    
}
