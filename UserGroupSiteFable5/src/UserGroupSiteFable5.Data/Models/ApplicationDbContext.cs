using UserGroupSiteFable5.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteFable5.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();
    public DbSet<TopicVote> TopicVotes => Set<TopicVote>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(es => new { es.EventId, es.UserId });
            entity.HasOne(es => es.Event)
                .WithMany(e => e.Speakers)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            // Restrict so users with speaker assignments cannot be deleted out from under events.
            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(ts => ts.SuggestedByUser)
                .WithMany()
                .HasForeignKey(ts => ts.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ts => ts.VolunteerUser)
                .WithMany()
                .HasForeignKey(ts => ts.VolunteerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicVote>(entity =>
        {
            // Composite key guarantees one vote per user per topic at the database level.
            entity.HasKey(tv => new { tv.TopicSuggestionId, tv.UserId });
            entity.HasOne(tv => tv.TopicSuggestion)
                .WithMany(ts => ts.Votes)
                .HasForeignKey(tv => tv.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tv => tv.User)
                .WithMany()
                .HasForeignKey(tv => tv.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed the well-known roles. Fixed ids/stamps keep the migration deterministic.
        builder.Entity<Role>().HasData(
            new Role
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "f2da4b54-0d9e-4f4d-bd9a-6e6b1bb976dd"
            },
            new Role
            {
                Id = 2,
                Name = "Speaker",
                NormalizedName = "SPEAKER",
                ConcurrencyStamp = "9a1cfa07-1f7a-44a6-8e0a-2cb9be9c8a5e"
            });
    }
}
