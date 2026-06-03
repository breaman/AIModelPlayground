namespace UserGroupSiteSonnet46.Shared.Services;

/// <summary>Represents a topic suggestion for display, including vote and volunteer status.</summary>
public record TopicSuggestionDto(
    int Id,
    string Title,
    string? Description,
    UserDto SuggestedBy,
    int VoteCount,
    bool HasCurrentUserVoted,
    UserDto? Volunteer);

/// <summary>
/// Service for topic suggestions, voting, and volunteering to present a topic.
/// </summary>
public interface ITopicService
{
    /// <summary>Returns all topic suggestions, including current-user vote state.</summary>
    Task<List<TopicSuggestionDto>> GetTopicsAsync();

    /// <summary>Creates a new topic suggestion on behalf of the current user.</summary>
    Task<TopicSuggestionDto> SuggestTopicAsync(string title, string? description);

    /// <summary>
    /// Records a vote for the specified topic. Idempotent — no-op if the user already voted.
    /// </summary>
    Task VoteAsync(int topicId);

    /// <summary>
    /// Registers the current user as the volunteer for a topic.
    /// Throws if the topic already has a volunteer.
    /// </summary>
    Task VolunteerAsync(int topicId);
}
