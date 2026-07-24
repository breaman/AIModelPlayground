using UserGroupSiteGpt56Sol.Shared.Models;

namespace UserGroupSiteGpt56Sol.Shared.Services;

/// <summary>Provides authenticated topic suggestion operations.</summary>
public interface ITopicService
{
    Task<IReadOnlyList<TopicSuggestionDto>> GetAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(CreateTopicRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> VoteAsync(int topicId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnvoteAsync(int topicId, CancellationToken cancellationToken = default);
    Task<ServiceResult> VolunteerAsync(int topicId, CancellationToken cancellationToken = default);
}