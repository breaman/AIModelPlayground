using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt56Sol.Data.Models;

/// <summary>Represents a technical user group event.</summary>
public sealed class Event : FingerPrintEntityBase
{
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    [MaxLength(200)]
    public required string NormalizedSlug { get; set; }

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [MaxLength(20_000)]
    public string? DescriptionMarkdown { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}