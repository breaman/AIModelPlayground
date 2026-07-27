using UserGroupSiteOpus5.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserGroupSiteOpus5.Data.Configurations;

/// <summary>EF Core mapping for <see cref="EventSpeaker"/>.</summary>
public class EventSpeakerConfiguration : IEntityTypeConfiguration<EventSpeaker>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventSpeaker> builder)
    {
        builder.HasOne(x => x.Event)
            .WithMany(x => x.Speakers)
            .HasForeignKey(x => x.EventId)
            // Removing an event should take its speaker assignments with it.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            // Deleting a user must not silently erase event history.
            .OnDelete(DeleteBehavior.Restrict);

        // A user is listed at most once per event.
        builder.HasIndex(x => new { x.EventId, x.UserId })
            .IsUnique();
    }
}
