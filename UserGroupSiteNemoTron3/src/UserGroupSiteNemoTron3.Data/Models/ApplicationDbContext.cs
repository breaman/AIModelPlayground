using Microsoft.EntityFrameworkCore;

using UserGroupSiteNemoTron3.Data.Interfaces;

namespace UserGroupSiteNemoTron3.Data.Models;

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
        base.OnModelCreating(builder);

        // Event configuration
        builder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasMany(e => e.EventSpeakers)
                .WithOne(es => es.Event)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EventSpeaker configuration
        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TopicSuggestion configuration
        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(ts => ts.VolunteerSpeaker)
                .WithMany()
                .HasForeignKey(ts => ts.VolunteerSpeakerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(ts => ts.Votes)
                .WithOne(v => v.TopicSuggestion)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TopicVote configuration
        builder.Entity<TopicVote>(entity =>
        {
            entity.HasIndex(v => new { v.TopicSuggestionId, v.UserId }).IsUnique();
            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}