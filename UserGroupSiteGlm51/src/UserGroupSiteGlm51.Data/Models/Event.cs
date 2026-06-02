using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm51.Data.Models;

/// <summary>
/// Represents a user group meeting event.
/// </summary>
public class Event : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Full event description in Markdown format.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date and time of the event, stored as DateTimeOffset for timezone awareness.
    /// </summary>
    public DateTimeOffset? EventDate { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// Whether the event is visible to the public. Defaults to false (draft).
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Navigation property for speakers assigned to this event.
    /// </summary>
    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}