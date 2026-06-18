using Microsoft.EntityFrameworkCore;

using UserGroupSiteQwen35.Data.Interfaces;

namespace UserGroupSiteQwen35.Data.Models;

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
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();
    public DbSet<TopicVote> TopicVotes => Set<TopicVote>();
    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(es => new { es.EventId, es.SpeakerId });
            entity.HasOne(es => es.Event).WithMany().HasForeignKey(es => es.EventId);
            entity.HasOne(es => es.Speaker).WithMany().HasForeignKey(es => es.SpeakerId);
        });

        modelBuilder.Entity<TopicVote>(entity =>
        {
            entity.HasIndex(tv => new { tv.TopicSuggestionId, tv.UserId }).IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
        });
    }
}