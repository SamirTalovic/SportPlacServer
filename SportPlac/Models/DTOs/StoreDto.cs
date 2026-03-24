namespace SportPlac.Models.DTOs
{
    public class StoreDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public int TotalSales { get; set; }
        public double ResponseRate { get; set; }

        public int ListingsCount { get; set; }
        public int ReviewsCount { get; set; }
    }
    public class UpdateStoreDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }


}
