namespace UserGroupSiteGlm51.Shared.Models;

/// <summary>
/// DTO for returning topic suggestion data to clients.
/// </summary>
public record TopicSuggestionDto(
    int Id,
    string Title,
    string? Description,
    int SuggestedById,
    string? SuggestedByName,
    int? VolunteerId,
    string? VolunteerName,
    int VoteCount,
    bool HasVoted,
    bool IsVolunteer
);

/// <summary>
/// Request body for creating a new topic suggestion.
/// </summary>
public record CreateTopicRequest(
    string Title,
    string? Description
);