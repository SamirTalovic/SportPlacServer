namespace SportPlac.Models.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string? Phone { get; set; }
        public string? City { get; set; }

        public string StoreName { get; set; }
    }
}
