using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt55.Data.Models.Events;

/// <summary>
/// Represents a user group event that can be drafted, edited, and published.
/// </summary>
public sealed class UserGroupEvent : FingerPrintEntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(220)]
    public string Slug { get; set; } = "";

    [MaxLength(300)]
    public string? ShortDescription { get; set; }

    public string? MarkdownDescription { get; set; }

    public DateTimeOffset? StartsAt { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public List<EventSpeaker> EventSpeakers { get; set; } = [];
}