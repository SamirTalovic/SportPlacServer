namespace SportPlac.Models
{
    public class ConversationParticipant
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public int UnreadCount { get; set; } = 0;

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
