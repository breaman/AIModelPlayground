using System.ComponentModel.DataAnnotations;

using UserGroupSiteOpus5.Shared.Common;

namespace UserGroupSiteOpus5.Shared.Models;

/// <summary>A speaker as shown against an event or offered for assignment.</summary>
/// <param name="UserId">The speaker's user id.</param>
/// <param name="FirstName">The speaker's first name.</param>
/// <param name="LastName">The speaker's last name.</param>
public record EventSpeakerInfo(int UserId, string? FirstName, string? LastName)
{
    /// <summary>The speaker's full name, falling back to a placeholder when no name is recorded.</summary>
    public string DisplayName =>
        string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } name
            ? name
            : "Unnamed member";
}

/// <summary>An event as shown in a listing.</summary>
/// <param name="Id">The event id.</param>
/// <param name="Title">The event title.</param>
/// <param name="Slug">The event's URL slug.</param>
/// <param name="ShortDescription">The listing teaser.</param>
/// <param name="EventDateTime">When the meeting takes place, if scheduled.</param>
/// <param name="Location">The venue.</param>
/// <param name="IsPublished">Whether the event is publicly visible.</param>
/// <param name="Speakers">The assigned speakers.</param>
public record EventListItem(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    DateTimeOffset? EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<EventSpeakerInfo> Speakers);

/// <summary>An event as shown on its own page.</summary>
/// <param name="Id">The event id.</param>
/// <param name="Title">The event title.</param>
/// <param name="Slug">The event's URL slug.</param>
/// <param name="ShortDescription">The listing teaser.</param>
/// <param name="Description">The full description, as Markdown source.</param>
/// <param name="EventDateTime">When the meeting takes place, if scheduled.</param>
/// <param name="Location">The venue.</param>
/// <param name="IsPublished">Whether the event is publicly visible.</param>
/// <param name="Speakers">The assigned speakers.</param>
/// <param name="CanEdit">Whether the current viewer may edit this event.</param>
public record EventDetail(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<EventSpeakerInfo> Speakers,
    bool CanEdit);

/// <summary>
/// The form-bound model for creating and editing an event.
/// </summary>
/// <remarks>
/// A class rather than a record because Blazor's two-way binding needs settable properties.
/// Implements <see cref="IValidatableObject"/> to express the publish preconditions, which are
/// cross-field rules and so cannot be stated with per-property attributes. Blazor runs
/// per-property validation on field change and the full object — including this method — on
/// submit, which is the right moment to tell someone their event is not ready to publish.
/// </remarks>
public class EventEditModel : IValidatableObject
{
    /// <summary>The event id; zero when creating.</summary>
    public int Id { get; set; }

    /// <summary>Display title. Always required.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(FieldLengths.EventTitle)]
    [Display(Name = "Title")]
    public string Title { get; set; } = "";

    /// <summary>URL slug. Always required; auto-populated from the title when left blank.</summary>
    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(FieldLengths.Slug)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = "";

    /// <summary>Listing teaser.</summary>
    [MaxLength(FieldLengths.ShortDescription)]
    [Display(Name = "Short description")]
    public string? ShortDescription { get; set; }

    /// <summary>Full description as Markdown source.</summary>
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>When the meeting takes place.</summary>
    [Display(Name = "Date and time")]
    public DateTimeOffset? EventDateTime { get; set; }

    /// <summary>Venue and room.</summary>
    [MaxLength(FieldLengths.Location)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    /// <summary>Whether to make the event publicly visible.</summary>
    [Display(Name = "Published")]
    public bool IsPublished { get; set; }

    /// <summary>The user ids of the assigned speakers.</summary>
    public List<int> SpeakerUserIds { get; set; } = [];

    /// <summary>
    /// Enforces the publish preconditions: a published event must have a description, a date, a
    /// location, and at least one speaker. Drafts are exempt so an event can be roughed out with
    /// nothing but a title.
    /// </summary>
    /// <param name="validationContext">The validation context. Unused.</param>
    /// <returns>One result per unmet precondition.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPublished)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult(
                "A description is required to publish an event.", [nameof(Description)]);
        }

        if (EventDateTime is null)
        {
            yield return new ValidationResult(
                "A date and time is required to publish an event.", [nameof(EventDateTime)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult(
                "A location is required to publish an event.", [nameof(Location)]);
        }

        if (SpeakerUserIds.Count == 0)
        {
            yield return new ValidationResult(
                "At least one speaker is required to publish an event.", [nameof(SpeakerUserIds)]);
        }
    }
}
