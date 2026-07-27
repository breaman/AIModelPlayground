namespace UserGroupSiteOpus5.Data.Models;

/// <summary>
/// A user group meeting. Events are created as drafts and only appear publicly once
/// <see cref="IsPublished"/> is set, which requires the description, date, location, and at least
/// one speaker to be filled in.
/// </summary>
public class Event : FingerPrintEntityBase
{
    /// <summary>Display title, e.g. "Minimal APIs in .NET 10".</summary>
    public string Title { get; set; } = "";

    /// <summary>URL-safe identifier, unique across all events. Generated from the title.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Teaser shown in listings. Plain text.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Full description stored as Markdown source; rendered to HTML for display.</summary>
    public string? Description { get; set; }

    /// <summary>When the meeting takes place. Null while the event is still being scheduled.</summary>
    public DateTimeOffset? EventDateTime { get; set; }

    /// <summary>Venue and room.</summary>
    public string? Location { get; set; }

    /// <summary>Whether the event is visible to anonymous visitors.</summary>
    public bool IsPublished { get; set; }

    /// <summary>The users presenting at this event.</summary>
    public ICollection<EventSpeaker> Speakers { get; set; } = [];
}
