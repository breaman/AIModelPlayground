using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt56Sol.Shared.Models;

/// <summary>Displays a published event in a list.</summary>
public sealed record EventSummaryDto(int Id, string Title, string Slug, string? ShortDescription,
    DateTime StartsAtUtc, string Location, IReadOnlyList<string> Speakers);

/// <summary>Displays a complete published event.</summary>
public sealed record EventDetailDto(int Id, string Title, string Slug, string? ShortDescription,
    string DescriptionHtml, DateTime StartsAtUtc, string Location, IReadOnlyList<string> Speakers);

/// <summary>Displays an event in the management list.</summary>
public sealed record EventAdminListItemDto(int Id, string Title, string Slug, DateTime? StartsAtUtc,
    bool IsPublished, bool CanManageSpeakers);

/// <summary>Represents a user eligible to speak at events.</summary>
public sealed record SpeakerOptionDto(int Id, string DisplayName, string Email);

/// <summary>Represents editable event values.</summary>
public sealed class EventEditRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(200), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers, and single hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [StringLength(20_000)]
    public string? DescriptionMarkdown { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }
    public List<int> SpeakerIds { get; set; } = [];
    public string? RowVersion { get; set; }

    /// <summary>Enforces the additional fields required before an event can be published.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPublished)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(DescriptionMarkdown))
        {
            yield return new ValidationResult("A description is required to publish.",
                [nameof(DescriptionMarkdown)]);
        }

        if (StartsAtUtc is null)
        {
            yield return new ValidationResult("A date and time are required to publish.",
                [nameof(StartsAtUtc)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult("A location is required to publish.", [nameof(Location)]);
        }

        if (SpeakerIds.Count == 0)
        {
            yield return new ValidationResult("At least one speaker is required to publish.",
                [nameof(SpeakerIds)]);
        }
    }
}
