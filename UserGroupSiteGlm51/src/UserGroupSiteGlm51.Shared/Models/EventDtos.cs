namespace UserGroupSiteGlm51.Shared.Models;

/// <summary>
/// DTO for returning event data to clients.
/// </summary>
public record EventDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDate,
    string? Location,
    bool IsPublished,
    List<EventSpeakerDto> Speakers
);

/// <summary>
/// DTO for a speaker associated with an event.
/// </summary>
public record EventSpeakerDto(
    int UserId,
    string? FirstName,
    string? LastName,
    string? Email
);

/// <summary>
/// Request body for creating a new event.
/// </summary>
public record CreateEventRequest(
    string Title,
    string? Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDate,
    string? Location,
    bool IsPublished,
    List<int> SpeakerIds
);

/// <summary>
/// Request body for updating an existing event.
/// </summary>
public record UpdateEventRequest(
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDate,
    string? Location,
    bool IsPublished,
    List<int> SpeakerIds
);