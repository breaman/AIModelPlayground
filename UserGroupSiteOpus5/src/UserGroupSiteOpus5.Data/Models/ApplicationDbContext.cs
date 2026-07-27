using UserGroupSiteOpus5.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    /// <summary>User group meetings.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Speaker assignments linking users to events.</summary>
    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();

    /// <summary>Member-suggested topics for future meetings.</summary>
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();

    /// <summary>Member votes cast for topic suggestions.</summary>
    public DbSet<TopicSuggestionVote> TopicSuggestionVotes => Set<TopicSuggestionVote>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity's own mapping is applied by the base call and must run first.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
