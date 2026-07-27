using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserGroupSiteOpus5.Data.Configurations;

/// <summary>EF Core mapping for <see cref="TopicSuggestion"/>.</summary>
public class TopicSuggestionConfiguration : IEntityTypeConfiguration<TopicSuggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TopicSuggestion> builder)
    {
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(FieldLengths.TopicTitle);

        builder.Property(x => x.Description)
            .HasMaxLength(FieldLengths.Description);

        builder.HasOne(x => x.SuggestedByUser)
            .WithMany()
            .HasForeignKey(x => x.SuggestedByUserId)
            // Deleting a user must not silently erase the suggestions attributed to them.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VolunteerUser)
            .WithMany()
            .HasForeignKey(x => x.VolunteerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
