namespace SportPlac.Models
{
    public class SiteSettings
    {
        public Guid Id { get; set; }
        public string? MetaPixelId { get; set; }
        public string? GooglePixelId { get; set; }
        public bool AutoWebPConversion { get; set; }
        public bool SeoImageRenamer { get; set; }
        public bool BancaIntesaActive { get; set; }
        public string? BancaIntesaMerchantId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
