using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteOpus48.Data.Models;

/// <summary>
/// A user group meeting/event. Auditable via <see cref="FingerPrintEntityBase"/>.
/// </summary>
public class Event : FingerPrintEntityBase
{
    /// <summary>Display title of the event. Required.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-friendly unique identifier (kebab-case). Required and unique.</summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short summary shown in lists.</summary>
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>Full description stored as Markdown (rendered to HTML for display).</summary>
    public string? Description { get; set; }

    /// <summary>Scheduled date/time of the event. Required to publish.</summary>
    public DateTime? EventDateTime { get; set; }

    /// <summary>Physical or virtual location. Required to publish.</summary>
    [MaxLength(300)]
    public string? Location { get; set; }

    /// <summary>Whether the event is publicly visible. Publishing enforces completeness rules.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Speakers assigned to this event (join entity to <see cref="User"/>).</summary>
    public ICollection<EventSpeaker> Speakers { get; set; } = new List<EventSpeaker>();
}