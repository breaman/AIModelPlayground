using UserGroupSiteGpt56Sol.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Data.Models;

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

    /// <summary>Configures domain relationships and database constraints.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Event>(entity =>
        {
            entity.HasIndex(item => item.NormalizedSlug).IsUnique();
            entity.Property(item => item.RowVersion).IsRowVersion();
        });

        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(item => new { item.EventId, item.UserId });
            entity.HasOne(item => item.Event)
                .WithMany(item => item.EventSpeakers)
                .HasForeignKey(item => item.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User)
                .WithMany(item => item.SpeakingEvents)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasOne(item => item.Creator)
                .WithMany(item => item.CreatedTopicSuggestions)
                .HasForeignKey(item => item.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.VolunteerUser)
                .WithMany(item => item.VolunteeredTopicSuggestions)
                .HasForeignKey(item => item.VolunteerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicVote>(entity =>
        {
            entity.HasKey(item => new { item.TopicSuggestionId, item.UserId });
            entity.HasIndex(item => new { item.TopicSuggestionId, item.UserId }).IsUnique();
            entity.HasOne(item => item.TopicSuggestion)
                .WithMany(item => item.Votes)
                .HasForeignKey(item => item.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User)
                .WithMany(item => item.TopicVotes)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}