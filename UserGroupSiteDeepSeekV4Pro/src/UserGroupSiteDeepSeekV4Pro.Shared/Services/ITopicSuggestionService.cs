using UserGroupSiteDeepSeekV4Pro.Shared.Models;

namespace UserGroupSiteDeepSeekV4Pro.Shared.Services;

public interface ITopicSuggestionService
{
    Task<List<TopicSuggestionListItemDto>> GetAllAsync();
    Task<TopicSuggestionDto> CreateAsync(TopicSuggestionDto dto);
    Task VoteAsync(int topicId);
    Task RemoveVoteAsync(int topicId);
    Task VolunteerAsync(int topicId);
    Task RemoveVolunteerAsync(int topicId);
}
