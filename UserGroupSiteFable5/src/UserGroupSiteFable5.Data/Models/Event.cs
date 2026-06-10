using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Data.Models;

/// <summary>
/// A user group meeting/event. Created by admins; speakers are assigned via
/// <see cref="EventSpeaker"/> so editor authorization can query assignments directly.
/// </summary>
public class Event : FingerPrintEntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>Markdown source for the full event description.</summary>
    public string? Description { get; set; }

    public DateTime? StartsAt { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> Speakers { get; set; } = [];
}
