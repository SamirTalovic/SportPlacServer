namespace SportPlac.Models.DTOs
{
    public class UpdateSettingsDto
    {
        public string? MetaPixelId { get; set; }
        public string? GooglePixelId { get; set; }
        public bool AutoWebPConversion { get; set; }
        public bool SeoImageRenamer { get; set; }
        public bool BancaIntesaActive { get; set; }
        public string? BancaIntesaMerchantId { get; set; }
    }

}
