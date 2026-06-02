using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<EventSpeaker> EventSpeakers { get; set; } = null!;
    public DbSet<TopicSuggestion> TopicSuggestions { get; set; } = null!;
    public DbSet<TopicVote> TopicVotes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        builder.Entity<EventSpeaker>(entity =>
        {
            entity.HasKey(es => new { es.EventId, es.UserId });

            entity.HasOne(es => es.Event)
                .WithMany(e => e.EventSpeakers)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(es => es.User)
                .WithMany()
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TopicSuggestion>(entity =>
        {
            entity.HasOne(ts => ts.SuggestedByUser)
                .WithMany()
                .HasForeignKey(ts => ts.SuggestedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ts => ts.VolunteerUser)
                .WithMany()
                .HasForeignKey(ts => ts.VolunteerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TopicVote>(entity =>
        {
            entity.HasKey(tv => new { tv.TopicSuggestionId, tv.UserId });

            entity.HasOne(tv => tv.TopicSuggestion)
                .WithMany(ts => ts.Votes)
                .HasForeignKey(tv => tv.TopicSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tv => tv.User)
                .WithMany()
                .HasForeignKey(tv => tv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}