using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Shared.Dtos;

/// <summary>Row data for event lists (editor list and admin overview).</summary>
public class EventSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public List<string> SpeakerNames { get; set; } = [];
}

/// <summary>Full event details for public display.</summary>
public class EventDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime? StartsAt { get; set; }
    public string? Location { get; set; }
    public List<string> SpeakerNames { get; set; } = [];
}

/// <summary>
/// Editable event payload. Publish-time requirements are enforced via
/// <see cref="IValidatableObject"/> so the WASM form and server endpoint validate identically.
/// </summary>
public class EventEditDto : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public DateTime? StartsAt { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public List<int> SpeakerUserIds { get; set; } = [];

    /// <summary>A published event must be complete enough to show to the public.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPublished)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("A description is required to publish an event.",
                [nameof(Description)]);
        }

        if (StartsAt is null)
        {
            yield return new ValidationResult("A date and time is required to publish an event.",
                [nameof(StartsAt)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult("A location is required to publish an event.",
                [nameof(Location)]);
        }

        if (SpeakerUserIds.Count == 0)
        {
            yield return new ValidationResult("At least one speaker is required to publish an event.",
                [nameof(SpeakerUserIds)]);
        }
    }
}