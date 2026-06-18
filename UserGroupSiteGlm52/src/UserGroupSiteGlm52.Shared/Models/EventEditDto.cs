using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>
/// Mutable model backing the event editor form. Carries DataAnnotations for
/// client-side validation; cross-field/business rules (e.g. publish requires
/// description/date/location/speaker) are enforced server-side.
/// </summary>
public sealed class EventEditDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = "";

    [MaxLength(300)]
    public string? ShortDescription { get; set; }

    /// <summary>Markdown source.</summary>
    public string? Description { get; set; }

    public DateTime? EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Ids of users assigned as speakers.</summary>
    public List<int> SpeakerIds { get; set; } = [];

    /// <summary>Available speakers (users in the Speaker role) for the picker.</summary>
    public IReadOnlyList<SpeakerOptionDto> SpeakerOptions { get; set; } = [];
}