using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Interfaces;
using UserGroupSiteGpt55.Data.Models.Events;
using UserGroupSiteGpt55.Data.Models.Topics;

namespace UserGroupSiteGpt55.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    public DbSet<UserGroupEvent> Events => Set<UserGroupEvent>();

    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();

    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();

    public DbSet<TopicVote> TopicVotes => Set<TopicVote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserGroupEvent>(entity =>
        {
            entity.ToTable("Events");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.StartsAt).HasPrecision(0);
        });

        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(e => new { e.EventId, e.UserId });
            entity.HasOne(e => e.Event)
                .WithMany(e => e.EventSpeakers)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.CreatedOn).HasPrecision(0);
            entity.HasOne(e => e.SuggestedByUser)
                .WithMany()
                .HasForeignKey(e => e.SuggestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.VolunteerSpeakerUser)
                .WithMany()
                .HasForeignKey(e => e.VolunteerSpeakerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TopicVote>(entity =>
        {
            entity.HasKey(e => new { e.TopicSuggestionId, e.UserId });
            entity.Property(e => e.CreatedOn).HasPrecision(0);
            entity.HasOne(e => e.TopicSuggestion)
                .WithMany(e => e.Votes)
                .HasForeignKey(e => e.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
