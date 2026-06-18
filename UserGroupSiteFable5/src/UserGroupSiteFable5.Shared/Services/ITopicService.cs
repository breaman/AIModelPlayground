using UserGroupSiteFable5.Shared.Dtos;

namespace UserGroupSiteFable5.Shared.Services;

/// <summary>
/// Topic suggestion operations for any authenticated member. Dual-implemented
/// (HTTP client / database server) per the WASM pre-rendering pattern.
/// </summary>
public interface ITopicService
{
    /// <summary>All suggestions sorted by vote count descending.</summary>
    Task<List<TopicSuggestionDto>> GetTopicsAsync();

    Task<ServiceResult> CreateTopicAsync(TopicCreateDto topic);

    /// <summary>Casts the current user's vote; fails with Conflict when already voted.</summary>
    Task<ServiceResult> VoteAsync(int topicId);

    /// <summary>Claims the single volunteer slot; fails with Conflict when already claimed.</summary>
    Task<ServiceResult> VolunteerAsync(int topicId);
}