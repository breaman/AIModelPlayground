using Microsoft.EntityFrameworkCore;

using UserGroupSiteMiniMaxM3.Data.Interfaces;

namespace UserGroupSiteMiniMaxM3.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    /// <summary>User-group events (talks, meetups). Speakers are configured in <see cref="OnModelCreating"/>.</summary>
    public DbSet<GroupEvent> Events => Set<GroupEvent>();

    /// <summary>Topics suggested by users for future meetups.</summary>
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();

    /// <summary>One row per (user, topic) vote.</summary>
    public DbSet<TopicVote> TopicVotes => Set<TopicVote>();

    /// <summary>One row per (user, topic) volunteer offer.</summary>
    public DbSet<TopicVolunteer> TopicVolunteers => Set<TopicVolunteer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // GroupEvent: unique slug, many-to-many speakers via EventSpeakers join table.
        // We do NOT add a global query filter for IsPublished so the intent stays at the
        // call site; the public list explicitly filters IsPublished == true.
        modelBuilder.Entity<GroupEvent>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(250);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(250);

            // Many-to-many: a user can speak at many events, an event has many speakers.
            // Restrict cascade on user delete so we don't silently lose event records.
            entity.HasMany(e => e.Speakers)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "EventSpeakers",
                    right => right.HasOne<User>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<GroupEvent>()
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("EventId", "UserId");
                        join.ToTable("EventSpeakers");
                        join.HasIndex("UserId");
                    });
        });

        // TopicSuggestion: votes and volunteers are modeled as explicit join entities
        // so we can attach a CreatedAt timestamp and configure cascade behavior.
        modelBuilder.Entity<TopicSuggestion>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.CreatedOn);

            // Restrict cascade so deleting a user doesn't silently drop their topic.
            entity.HasOne(t => t.SuggestedByUser)
                .WithMany()
                .HasForeignKey(t => t.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TopicVote>(entity =>
        {
            entity.HasKey(v => new { v.TopicSuggestionId, v.UserId });
            entity.HasIndex(v => v.UserId);

            entity.HasOne(v => v.Topic)
                .WithMany(t => t.Votes)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TopicVolunteer>(entity =>
        {
            entity.HasKey(v => new { v.TopicSuggestionId, v.UserId });
            entity.HasIndex(v => v.UserId);

            entity.HasOne(v => v.Topic)
                .WithMany(t => t.Volunteers)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}