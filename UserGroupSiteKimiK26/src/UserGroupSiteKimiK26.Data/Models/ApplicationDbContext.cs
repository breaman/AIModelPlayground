using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK26.Data.Interfaces;

namespace UserGroupSiteKimiK26.Data.Models;

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
            entity.HasOne(es => es.Event)
                .WithMany(e => e.EventSpeakers)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(es => new { es.EventId, es.UserId }).IsUnique();
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(t => t.SuggestedByUser)
                .WithMany()
                .HasForeignKey(t => t.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.VolunteerUser)
                .WithMany()
                .HasForeignKey(t => t.VolunteerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TopicVote>(entity =>
        {
            entity.HasOne(v => v.TopicSuggestion)
                .WithMany(t => t.Votes)
                .HasForeignKey(v => v.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(v => new { v.TopicSuggestionId, v.UserId }).IsUnique();
        });
    }
}