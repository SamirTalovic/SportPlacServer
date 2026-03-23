namespace SportPlac.Models
{
    // ==========================================
    // ENUMS
    // ==========================================

    public enum UserStatus
    {
        Active,
        Suspended,
        Banned
    }

    public enum SubscriptionPlan
    {
        Free,
        Premium
    }

    public enum ListingStatus
    {
        Active,
        Inactive,
        Reported,
        Expired
    }

    public enum ItemCondition
    {
        Novo,
        KaoNov,
        Korisceno,
        Osteceno
    }

    public enum NotificationType
    {
        Message,
        Like,
        Promo,
        Review,
        System
    }

    public enum AuthProvider
    {
        Email,
        Google,
        Facebook,
        Apple
    }

    public enum AppRole { User, Admin, Moderator }


}
