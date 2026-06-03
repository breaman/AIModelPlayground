using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Events;

/// <summary>
/// Form model for event create/edit. Validation rules enforce that
/// published events must have description, date/time, location, and at least one speaker.
/// </summary>
public class EventEditModel : IValidatableObject
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(200)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug must be lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public DateOnly? EventDate { get; set; }

    public TimeOnly? EventTime { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public List<int> SpeakerIds { get; set; } = [];

    public bool IsPublished { get; set; }

    /// <summary>Combines <see cref="EventDate"/> and <see cref="EventTime"/> into a <see cref="DateTimeOffset"/>.</summary>
    public DateTimeOffset? CombinedEventDateTime =>
        EventDate.HasValue
            ? new DateTimeOffset(EventDate.Value.ToDateTime(EventTime ?? TimeOnly.MinValue), TimeSpan.Zero)
            : null;

    /// <summary>
    /// Additional validation: published events must have description, date, location, and at least one speaker.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPublished)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("Description is required when publishing.", [nameof(Description)]);
        }

        if (!EventDate.HasValue)
        {
            yield return new ValidationResult("Event date is required when publishing.", [nameof(EventDate)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult("Location is required when publishing.", [nameof(Location)]);
        }

        if (SpeakerIds.Count == 0)
        {
            yield return new ValidationResult("At least one speaker is required when publishing.", [nameof(SpeakerIds)]);
        }
    }
}
