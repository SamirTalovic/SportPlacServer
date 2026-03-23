namespace SportPlac.Models
{
    public class ListingTag
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string Tag { get; set; } = string.Empty;

        public Listing Listing { get; set; } = null!;
    }
}
