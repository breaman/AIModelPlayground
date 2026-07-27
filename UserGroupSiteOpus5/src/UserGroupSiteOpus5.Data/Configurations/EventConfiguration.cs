using UserGroupSiteOpus5.Data.Common;
using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserGroupSiteOpus5.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Event"/>.</summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(FieldLengths.EventTitle);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(FieldLengths.Slug);

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(FieldLengths.ShortDescription);

        // Markdown source is unbounded; the rendered output is produced at display time.
        builder.Property(x => x.Description)
            .HasColumnType(ColumnTypes.MarkdownText);

        builder.Property(x => x.Location)
            .HasMaxLength(FieldLengths.Location);

        builder.Property(x => x.IsPublished)
            .HasDefaultValue(false);

        // The unique index is the real guarantee that two events never share a URL; slug
        // generation only tries to avoid the collision.
        builder.HasIndex(x => x.Slug)
            .IsUnique();

        // Published listings filter on this flag and sort by date.
        builder.HasIndex(x => new { x.IsPublished, x.EventDateTime });
    }
}
