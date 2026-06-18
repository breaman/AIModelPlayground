using Microsoft.EntityFrameworkCore;

using UserGroupSiteSonnet46.Data.Interfaces;

namespace UserGroupSiteSonnet46.Data.Models;

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

        // Composite PK for the event-speaker join table
        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(es => new { es.EventId, es.UserId });

            entity.HasOne(es => es.Event)
                .WithMany(e => e.Speakers)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict so that deleting a user does not cascade-delete events
            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(t => t.SuggestedBy)
                .WithMany()
                .HasForeignKey(t => t.SuggestedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Volunteer)
                .WithMany()
                .HasForeignKey(t => t.VolunteerUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // Composite PK ensures one vote per user per topic
        builder.Entity<TopicVote>(entity =>
        {
            entity.HasKey(v => new { v.TopicSuggestionId, v.UserId });

            entity.HasOne(v => v.TopicSuggestion)
                .WithMany(t => t.Votes)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}