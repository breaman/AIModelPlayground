using UserGroupSiteMiniMaxM3.Shared.Models.Topics;

namespace UserGroupSiteMiniMaxM3.Shared.Services;

/// <summary>
/// Cross-platform contract for topic operations. The Server implementation
/// talks to the database; the Client implementation calls the HTTP API.
/// </summary>
public interface ITopicService
{
    /// <summary>Lists all topics with vote/volunteer counts and the current user's state.</summary>
    Task<IReadOnlyList<TopicSummaryDto>> ListAsync(string? currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new topic suggestion. Throws on validation failure.</summary>
    Task<TopicSummaryDto> CreateAsync(TopicCreateDto input, string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Toggles the requesting user's vote. Returns the new state (true = voted).</summary>
    Task<bool> ToggleVoteAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Toggles the requesting user's volunteer offer. Returns the new state (true = volunteering).</summary>
    Task<bool> ToggleVolunteerAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a topic. Only the suggester or an admin can delete.</summary>
    Task DeleteAsync(int topicId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}