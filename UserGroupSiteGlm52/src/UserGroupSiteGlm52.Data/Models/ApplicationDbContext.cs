using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm52.Data.Interfaces;

namespace UserGroupSiteGlm52.Data.Models;

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
    public DbSet<TopicVolunteer> TopicVolunteers => Set<TopicVolunteer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Event: unique slug is the backstop for URL routing — no two events share a slug.
        builder.Entity<Event>(b =>
        {
            b.HasIndex(e => e.Slug).IsUnique();
            b.HasMany(e => e.Speakers)
                .WithOne(es => es.Event)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EventSpeaker: a user is assigned to an event at most once.
        builder.Entity<EventSpeaker>(b =>
        {
            b.HasIndex(es => new { es.EventId, es.UserId }).IsUnique();
            b.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TopicSuggestion: suggested-by user relationship + volunteer cascade.
        builder.Entity<TopicSuggestion>(b =>
        {
            b.HasOne(t => t.SuggestedByUser)
                .WithMany()
                .HasForeignKey(t => t.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(t => t.Votes)
                .WithOne(v => v.Topic)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.Volunteer)
                .WithOne(v => v.Topic)
                .HasForeignKey<TopicVolunteer>(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TopicVote: one vote per user per topic.
        builder.Entity<TopicVote>(b =>
        {
            b.HasIndex(v => new { v.TopicSuggestionId, v.UserId }).IsUnique();
            b.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // TopicVolunteer: at most one volunteer per topic.
        builder.Entity<TopicVolunteer>(b =>
        {
            b.HasIndex(v => v.TopicSuggestionId).IsUnique();
            b.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}