using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Interfaces;

public interface ITopicVoteRepository
{
    Task<TopicVote?> GetByUserAndTopicAsync(int userId, int topicId);
    Task<TopicVote> CreateAsync(TopicVote vote);
    Task DeleteAsync(TopicVote vote);
    Task<int> GetVoteCountAsync(int topicId);
}
