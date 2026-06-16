using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Interfaces;

namespace UserGroupSiteKimiK27Code.Data.Models;

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

        builder.Entity<Event>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<EventSpeaker>(e =>
        {
            e.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            e.HasOne(x => x.Event).WithMany(x => x.EventSpeakers).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TopicSuggestion>(e =>
        {
            e.HasOne(x => x.SuggestedBy).WithMany().HasForeignKey(x => x.SuggestedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VolunteerSpeaker).WithMany().HasForeignKey(x => x.VolunteerSpeakerId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TopicVote>(e =>
        {
            e.HasIndex(x => new { x.TopicSuggestionId, x.UserId }).IsUnique();
            e.HasOne(x => x.TopicSuggestion).WithMany(x => x.Votes).HasForeignKey(x => x.TopicSuggestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}