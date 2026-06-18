using UserGroupSiteKimiK26.Shared.Dtos;

namespace UserGroupSiteKimiK26.Shared.Services;

public interface ITopicSuggestionsService
{
    Task<List<TopicSuggestionDto>> GetAllSuggestionsAsync();
    Task<TopicSuggestionDto> CreateSuggestionAsync(CreateSuggestionDto dto);
    Task<VoteResultDto> VoteAsync(int topicId);
    Task<TopicSuggestionDto> VolunteerAsync(int topicId);
    Task<bool> DeleteSuggestionAsync(int topicId);
}