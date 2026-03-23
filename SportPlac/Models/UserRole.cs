namespace SportPlac.Models
{
    public class UserRole
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public AppRole Role { get; set; }
        public User User { get; set; }
    }
}
