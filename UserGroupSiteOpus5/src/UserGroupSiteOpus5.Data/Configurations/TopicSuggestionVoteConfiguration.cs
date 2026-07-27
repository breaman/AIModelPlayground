using UserGroupSiteOpus5.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserGroupSiteOpus5.Data.Configurations;

/// <summary>EF Core mapping for <see cref="TopicSuggestionVote"/>.</summary>
public class TopicSuggestionVoteConfiguration : IEntityTypeConfiguration<TopicSuggestionVote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TopicSuggestionVote> builder)
    {
        builder.HasOne(x => x.TopicSuggestion)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.TopicSuggestionId)
            // Removing a suggestion should take its votes with it.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enforces one vote per user per topic at the database level.
        builder.HasIndex(x => new { x.TopicSuggestionId, x.UserId })
            .IsUnique();
    }
}
