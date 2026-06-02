using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteMiniMaxM3.Data.Models;

/// <summary>
/// A user-group meeting: a scheduled talk or gathering with a title, location, optional
/// markdown description, and one or more speakers. <see cref="IsPublished"/> controls
/// whether anonymous visitors can see the event; unpublished events are visible only to
/// their editors (admins and assigned speakers).
/// </summary>
public class GroupEvent : FingerPrintEntityBase
{
    /// <summary>Primary key.</summary>
    public new int Id { get; set; }

    /// <summary>Event title shown on listings and the detail page.</summary>
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug used in <c>/events/{slug}</c>. Unique. Editors can override
    /// the auto-generated slug; once edited, the auto-fill stops.
    /// </summary>
    [Required, MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>One-paragraph summary shown on listings. Plain text, no markdown.</summary>
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>Full description in markdown. Rendered through Markdig server-side.</summary>
    public string? Description { get; set; }

    /// <summary>When the event takes place, stored as UTC.</summary>
    public DateTime EventDateTime { get; set; }

    /// <summary>Free-form location, e.g. "Conf Room B" or "https://meet.example/abc".</summary>
    [MaxLength(250)]
    public string? Location { get; set; }

    /// <summary>True when the event is visible to the public; false keeps it editor-only.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Speakers assigned to this event (many-to-many).</summary>
    public ICollection<User> Speakers { get; set; } = [];
}