using UserGroupSiteGlm51.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGlm51.Data.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event: Slug must be unique
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Slug)
            .IsUnique();

        // EventSpeaker: composite key on (EventId, UserId) — one speaker assignment per event per user
        modelBuilder.Entity<EventSpeaker>()
            .HasIndex(es => new { es.EventId, es.UserId })
            .IsUnique();

        modelBuilder.Entity<EventSpeaker>()
            .HasOne(es => es.Event)
            .WithMany(e => e.EventSpeakers)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventSpeaker>()
            .HasOne(es => es.User)
            .WithMany()
            .HasForeignKey(es => es.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // TopicSuggestion: VolunteerId is optional (nullable FK to User)
        modelBuilder.Entity<TopicSuggestion>()
            .HasOne(ts => ts.SuggestedBy)
            .WithMany()
            .HasForeignKey(ts => ts.SuggestedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TopicSuggestion>()
            .HasOne(ts => ts.Volunteer)
            .WithMany()
            .HasForeignKey(ts => ts.VolunteerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TopicSuggestion>()
            .HasMany(ts => ts.Votes)
            .WithOne(v => v.TopicSuggestion)
            .HasForeignKey(v => v.TopicSuggestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // TopicVote: composite key on (TopicSuggestionId, UserId) — one vote per user per topic
        modelBuilder.Entity<TopicVote>()
            .HasIndex(tv => new { tv.TopicSuggestionId, tv.UserId })
            .IsUnique();

        modelBuilder.Entity<TopicVote>()
            .HasOne(tv => tv.User)
            .WithMany()
            .HasForeignKey(tv => tv.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}