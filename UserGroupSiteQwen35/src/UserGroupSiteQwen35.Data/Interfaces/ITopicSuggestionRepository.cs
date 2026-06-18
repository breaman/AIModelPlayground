using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Interfaces;

public interface ITopicSuggestionRepository
{
    Task<IEnumerable<TopicSuggestion>> GetAllAsync();
    Task<TopicSuggestion?> GetByIdAsync(int id);
    Task<TopicSuggestion> CreateAsync(TopicSuggestion suggestion);
    Task<TopicSuggestion> UpdateAsync(TopicSuggestion suggestion);
    Task DeleteAsync(int id);
    Task<IEnumerable<TopicSuggestion>> GetWithVoteCountsAsync();
    Task<TopicSuggestion?> GetWithVotesAndVolunteerAsync(int id);
}