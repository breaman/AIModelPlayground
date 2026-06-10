using UserGroupSiteNemoTron3.Shared.DTOs;

namespace UserGroupSiteNemoTron3.Shared.Services;

public interface ITopicSuggestionService
{
    Task<TopicSuggestionDto[]> GetAllAsync();
    Task<TopicSuggestionDto?> GetByIdAsync(int id);
    Task<TopicSuggestionDto> CreateAsync(CreateTopicSuggestionDto dto, int userId);
    Task VoteAsync(int topicId, int userId);
    Task RemoveVoteAsync(int topicId, int userId);
    Task VolunteerAsync(int topicId, int userId);
    Task<bool> HasUserVotedAsync(int topicId, int userId);
}