using Microsoft.EntityFrameworkCore;
using SportPlac.Models;

namespace SportPlac.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<ListingTag> ListingTags { get; set; }
        public DbSet<ListingReport> ListingReports { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================
            // USER
            // ======================
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();

                e.HasOne(u => u.Store)
                    .WithOne(s => s.User)
                    .HasForeignKey<Store>(s => s.UserId);

                e.HasOne(u => u.Subscription)
                    .WithOne(s => s.User)
                    .HasForeignKey<Subscription>(s => s.UserId);
            });

            // ======================
            // USER ROLE
            // ======================
            modelBuilder.Entity<UserRole>(e =>
            {
                e.HasIndex(r => new { r.UserId, r.Role }).IsUnique();

                e.HasOne(r => r.User)
                    .WithMany(u => u.Roles)
                    .HasForeignKey(r => r.UserId);

                e.Property(r => r.Role).HasConversion<string>();
            });

            // ======================
            // STORE
            // ======================
            modelBuilder.Entity<Store>(e =>
            {
                e.HasMany(s => s.Listings)
                    .WithOne(l => l.Store)
                    .HasForeignKey(l => l.StoreId);

                e.HasMany(s => s.Reviews)
                    .WithOne(r => r.Store)
                    .HasForeignKey(r => r.StoreId);
            });

            // ======================
            // LISTING
            // ======================
            modelBuilder.Entity<Listing>(e =>
            {
                e.HasOne(l => l.Category)
                    .WithMany(c => c.Listings)
                    .HasForeignKey(l => l.CategoryId);

                e.HasOne(l => l.Seller)
                    .WithMany(u => u.Listings)
                    .HasForeignKey(l => l.SellerId);

                e.HasMany(l => l.Images)
                    .WithOne(i => i.Listing)
                    .HasForeignKey(i => i.ListingId);

                e.HasMany(l => l.Tags)
                    .WithOne(t => t.Listing)
                    .HasForeignKey(t => t.ListingId);
            });

            // ======================
            // LIKE
            // ======================
            modelBuilder.Entity<Like>(e =>
            {
                e.HasOne(l => l.Listing)
                    .WithMany(li => li.Likes)
                    .HasForeignKey(l => l.ListingId);

                e.HasOne(l => l.Store)
                    .WithMany(s => s.Likes)
                    .HasForeignKey(l => l.StoreId);

                e.HasIndex(l => new { l.UserId, l.ListingId })
                    .IsUnique()
                    .HasFilter("[ListingId] IS NOT NULL");

                e.HasIndex(l => new { l.UserId, l.StoreId })
                    .IsUnique()
                    .HasFilter("[StoreId] IS NOT NULL");
            });

            // ======================
            // REVIEW
            // ======================
            modelBuilder.Entity<Review>(e =>
            {
                e.HasOne(r => r.Reviewer)
                    .WithMany(u => u.ReviewsGiven)
                    .HasForeignKey(r => r.ReviewerId);

                e.HasOne(r => r.Seller)
                    .WithMany(u => u.ReviewsReceived)
                    .HasForeignKey(r => r.SellerId);

                e.HasOne(r => r.Store)
                    .WithMany(s => s.Reviews)
                    .HasForeignKey(r => r.StoreId);
            });

            // ======================
            // LISTING REPORT
            // ======================
            modelBuilder.Entity<ListingReport>(e =>
            {
                e.HasOne(r => r.Listing)
                    .WithMany(l => l.Reports)
                    .HasForeignKey(r => r.ListingId);

                e.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId);
            });

            // ======================
            // CONVERSATION
            // ======================
            modelBuilder.Entity<ConversationParticipant>(e =>
            {
                e.HasOne(cp => cp.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(cp => cp.ConversationId);

                e.HasOne(cp => cp.User)
                    .WithMany(u => u.Conversations)
                    .HasForeignKey(cp => cp.UserId);

                e.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();
            });

            modelBuilder.Entity<Message>(e =>
            {
                e.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId);
            });

            // ======================
            // NOTIFICATION
            // ======================
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId);

                e.HasIndex(n => new { n.UserId, n.IsRead });
            });

            // ======================
            // ENUMS
            // ======================
            modelBuilder.Entity<User>()
                .Property(u => u.Status).HasConversion<string>();

            modelBuilder.Entity<Listing>()
                .Property(l => l.Status).HasConversion<string>();

            modelBuilder.Entity<Listing>()
                .Property(l => l.Condition).HasConversion<string>();

            modelBuilder.Entity<Subscription>()
                .Property(s => s.Plan).HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type).HasConversion<string>();

            modelBuilder.Entity<UserRole>()
                .Property(r => r.Role).HasConversion<string>();


            // ======================
            // 🔥 GLOBAL FIX (NAJBITNIJE)
            // ======================
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
