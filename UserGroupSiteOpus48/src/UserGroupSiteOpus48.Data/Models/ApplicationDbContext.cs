using Microsoft.EntityFrameworkCore;

using UserGroupSiteOpus48.Data.Interfaces;

namespace UserGroupSiteOpus48.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();
    public DbSet<TopicVote> TopicVotes => Set<TopicVote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity needs its own configuration applied first.
        base.OnModelCreating(builder);

        builder.Entity<Event>(entity =>
        {
            // Slugs are used in public URLs and must be globally unique.
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        builder.Entity<EventSpeaker>(entity =>
        {
            // A speaker can only be assigned to a given event once.
            entity.HasIndex(es => new { es.EventId, es.UserId }).IsUnique();

            entity.HasOne(es => es.Event)
                .WithMany(e => e.Speakers)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict deleting a user that is still assigned as a speaker.
            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(t => t.SuggestedBy)
                .WithMany()
                .HasForeignKey(t => t.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Volunteer is optional; clearing the user should not cascade-delete topics.
            entity.HasOne(t => t.Volunteer)
                .WithMany()
                .HasForeignKey(t => t.VolunteerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicVote>(entity =>
        {
            // Enforce one vote per user per topic at the database level.
            entity.HasIndex(v => new { v.TopicSuggestionId, v.UserId }).IsUnique();

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