using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteOpus48.Shared.Dtos;

/// <summary>Summary of an event for list views (home page and editor list).</summary>
public record EventListItemDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    DateTime? EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<string> SpeakerNames);

/// <summary>Full event detail for the public detail page. <see cref="DescriptionHtml"/> is server-rendered Markdown.</summary>
public record EventDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? DescriptionHtml,
    DateTime? EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<SpeakerDto> Speakers);

/// <summary>
/// Editable event payload shared by the create/edit form, the client HTTP service, and the
/// server service. Implements <see cref="IValidatableObject"/> so the publish rules are validated
/// identically on the client (EditForm) and the server (re-validation).
/// </summary>
public class EventEditDto : IValidatableObject
{
    /// <summary>0 for a new event; otherwise the id being edited.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>Markdown source for the description.</summary>
    public string? Description { get; set; }

    public DateTime? EventDateTime { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Ids of users (in the Speaker role) assigned to this event.</summary>
    public List<int> SpeakerUserIds { get; set; } = new();

    /// <summary>
    /// Enforces the publish guard: a published event must have a description, date/time,
    /// location, and at least one speaker.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPublished)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("A published event requires a description.", [nameof(Description)]);
        }

        if (EventDateTime is null)
        {
            yield return new ValidationResult("A published event requires a date and time.", [nameof(EventDateTime)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult("A published event requires a location.", [nameof(Location)]);
        }

        if (SpeakerUserIds.Count == 0)
        {
            yield return new ValidationResult("A published event requires at least one speaker.", [nameof(SpeakerUserIds)]);
        }
    }
}

/// <summary>Identifies a saved event so the UI can navigate to it after create/update.</summary>
public record SavedEventDto(int Id, string Slug);