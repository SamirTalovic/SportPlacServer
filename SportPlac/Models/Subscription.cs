namespace SportPlac.Models
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
        public decimal? Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenewListings { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public User User { get; set; } = null!;
    }
}
