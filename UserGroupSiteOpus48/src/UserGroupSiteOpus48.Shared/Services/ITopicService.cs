using UserGroupSiteOpus48.Shared.Dtos;

namespace UserGroupSiteOpus48.Shared.Services;

/// <summary>
/// Topic suggestion operations (suggest, vote, volunteer). Dual-mode (Client HTTP / Server DB).
/// Vote/volunteer rules are enforced authoritatively on the server.
/// </summary>
public interface ITopicService
{
    /// <summary>All topic suggestions with vote counts, the current user's vote state, and volunteer.</summary>
    Task<TopicSuggestionDto[]> GetTopicsAsync();

    /// <summary>Suggests a new topic on behalf of the current user.</summary>
    Task<OperationResult> SuggestTopicAsync(TopicCreateDto dto);

    /// <summary>Casts the current user's single vote for a topic (idempotent: duplicates rejected).</summary>
    Task<OperationResult> VoteAsync(int topicId);

    /// <summary>Volunteers the current user to speak on a topic that has no volunteer yet.</summary>
    Task<OperationResult> VolunteerAsync(int topicId);
}