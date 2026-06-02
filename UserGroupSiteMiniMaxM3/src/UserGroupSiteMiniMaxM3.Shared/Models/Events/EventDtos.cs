namespace UserGroupSiteMiniMaxM3.Shared.Models.Events;

/// <summary>Read-only view of an event shown on listing pages and home.</summary>
/// <param name="Id">Event primary key.</param>
/// <param name="Title">Event title.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="ShortDescription">One-paragraph summary (plain text).</param>
/// <param name="EventDateTime">Event start (UTC; UI converts to local).</param>
/// <param name="Location">Free-form location string.</param>
/// <param name="IsPublished">Whether the event is publicly visible.</param>
/// <param name="SpeakerNames">Display names of the assigned speakers.</param>
public record EventSummaryDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    DateTime EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<string> SpeakerNames);

/// <summary>Read-only view of an event on the detail page, including the markdown body.</summary>
/// <param name="Id">Event primary key.</param>
/// <param name="Title">Event title.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="ShortDescription">One-paragraph summary.</param>
/// <param name="Description">Markdown body; rendered server-side via Markdig.</param>
/// <param name="EventDateTime">Event start (UTC).</param>
/// <param name="Location">Free-form location string.</param>
/// <param name="IsPublished">Whether the event is publicly visible.</param>
/// <param name="Speakers">Assigned speakers (id + display name).</param>
public record EventDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTime EventDateTime,
    string? Location,
    bool IsPublished,
    IReadOnlyList<EventSpeakerDto> Speakers);

/// <summary>A single speaker reference on an event.</summary>
/// <param name="Id">User id.</param>
/// <param name="DisplayName">"First Last" or the user name when name is missing.</param>
public record EventSpeakerDto(int Id, string DisplayName);

/// <summary>Edit-shape DTO used by the create/edit form. All fields are editable.</summary>
public class EventEditDto
{
    /// <summary>Event id; zero for new events.</summary>
    public int Id { get; set; }

    /// <summary>Event title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL slug; user-overridable.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>One-paragraph summary.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Markdown body.</summary>
    public string? Description { get; set; }

    /// <summary>Event start (local time coming from the form; converted to UTC by the service).</summary>
    public DateTime EventDateTime { get; set; }

    /// <summary>Free-form location string.</summary>
    public string? Location { get; set; }

    /// <summary>True to publish; false to keep editor-only.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Ids of the assigned speakers.</summary>
    public List<int> SpeakerIds { get; set; } = [];
}