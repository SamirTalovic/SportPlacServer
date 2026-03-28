namespace SportPlac.Models.DTOs
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string City { get; set; }

        public StoreDTO? Store { get; set; }
        public List<string> Roles { get; set; }

        public int ListingsCount { get; set; }
        public int ReviewsCount { get; set; }
    }



}
