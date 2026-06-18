using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteSonnet46.Data.Models;

/// <summary>
/// Represents a user group event with scheduling and publication details.
/// </summary>
public class Event : FingerPrintEntityBase
{
    /// <summary>Display title shown on event cards and the detail page.</summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>URL-friendly identifier; unique across all events.</summary>
    [MaxLength(200)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>Full Markdown body rendered on the detail page.</summary>
    public string? Description { get; set; }

    /// <summary>Required when <see cref="IsPublished"/> is true.</summary>
    public DateTimeOffset? EventDateTime { get; set; }

    /// <summary>Required when <see cref="IsPublished"/> is true.</summary>
    [MaxLength(300)]
    public string? Location { get; set; }

    /// <summary>Controls visibility on the public home page.</summary>
    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> Speakers { get; set; } = [];
}