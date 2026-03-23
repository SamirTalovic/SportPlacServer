namespace SportPlac.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public AuthProvider AuthProvider { get; set; } = AuthProvider.Email;
        public string? ExternalAuthId { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public Store? Store { get; set; }
        public Subscription? Subscription { get; set; }
        public ICollection<UserRole> Roles { get; set; }
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
        public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<ConversationParticipant> Conversations { get; set; } = new List<ConversationParticipant>();
    }

}
