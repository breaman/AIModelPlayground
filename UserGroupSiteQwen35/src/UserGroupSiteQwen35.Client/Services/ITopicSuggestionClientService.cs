using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Client.Services;

public interface ITopicSuggestionClientService
{
    Task<IEnumerable<TopicSuggestion>> GetAllSuggestionsAsync();
    Task<TopicSuggestion?> GetByIdAsync(int id);
    Task<TopicSuggestion> CreateSuggestionAsync(TopicSuggestion suggestion);
    Task<bool> VoteAsync(int topicId);
    Task<bool> RemoveVoteAsync(int topicId);
    Task<bool> HasVotedAsync(int topicId);
    Task<bool> VolunteerAsync(int topicId);
    Task<bool> RemoveVolunteerAsync(int topicId);
    Task<bool> IsUserVolunteerAsync(int topicId);
}