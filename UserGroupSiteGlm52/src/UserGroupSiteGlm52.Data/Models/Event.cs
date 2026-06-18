using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm52.Data.Models;

/// <summary>
/// A user group meeting event. Drafts (IsPublished = false) are only visible to
/// admins and assigned speakers; published events are shown to everyone on the
/// home page. The Description holds Markdown rendered to HTML on demand.
/// </summary>
public class Event : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    [MaxLength(300)]
    public string? ShortDescription { get; set; }

    /// <summary>Markdown source for the full event description.</summary>
    public string? Description { get; set; }

    /// <summary>Required when the event is published.</summary>
    public DateTime? EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Speakers assigned to the event (users in the Speaker role).</summary>
    public ICollection<EventSpeaker> Speakers { get; set; } = [];
}