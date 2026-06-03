using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Server.Services;

public class TopicSuggestionService
{
    private readonly ITopicSuggestionRepository _topicRepository;
    private readonly ITopicVoteRepository _voteRepository;
    private readonly ISpeakerRepository _speakerRepository;

    public TopicSuggestionService(
        ITopicSuggestionRepository topicRepository,
        ITopicVoteRepository voteRepository,
        ISpeakerRepository speakerRepository)
    {
        _topicRepository = topicRepository;
        _voteRepository = voteRepository;
        _speakerRepository = speakerRepository;
    }

    public async Task<IEnumerable<TopicSuggestion>> GetAllWithVoteCountsAsync()
    {
        return await _topicRepository.GetWithVoteCountsAsync();
    }

    public async Task<TopicSuggestion?> GetByIdAsync(int id)
    {
        return await _topicRepository.GetWithVotesAndVolunteerAsync(id);
    }

    public async Task<TopicSuggestion> CreateSuggestionAsync(TopicSuggestion suggestion)
    {
        suggestion.CreatedOn = DateTime.UtcNow;
        return await _topicRepository.CreateAsync(suggestion);
    }

    public async Task<bool> VoteAsync(int topicId, int userId)
    {
        var existing = await _voteRepository.GetByUserAndTopicAsync(userId, topicId);
        if (existing != null)
        {
            return false; // Already voted
        }

        var vote = new TopicVote
        {
            TopicSuggestionId = topicId,
            UserId = userId,
            VotedOn = DateTime.UtcNow
        };

        await _voteRepository.CreateAsync(vote);
        return true;
    }

    public async Task<bool> RemoveVoteAsync(int topicId, int userId)
    {
        var vote = await _voteRepository.GetByUserAndTopicAsync(userId, topicId);
        if (vote == null)
        {
            return false; // No vote to remove
        }

        await _voteRepository.DeleteAsync(vote);
        return true;
    }

    public async Task<bool> HasVotedAsync(int topicId, int userId)
    {
        var vote = await _voteRepository.GetByUserAndTopicAsync(userId, topicId);
        return vote != null;
    }

    public async Task<int> GetVoteCountAsync(int topicId)
    {
        return await _voteRepository.GetVoteCountAsync(topicId);
    }

    public async Task<bool> VolunteerAsync(int topicId, int userId)
    {
        var topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic == null || topic.VolunteerSpeakerId.HasValue)
        {
            return false; // Topic doesn't exist or already has a volunteer
        }

        // Check if user is already a speaker
        var existingSpeaker = await _speakerRepository.GetByUserIdAsync(userId);
        int speakerId;

        if (existingSpeaker != null)
        {
            speakerId = existingSpeaker.Id;
        }
        else
        {
            // Create a new speaker record
            var speaker = new Speaker
            {
                UserId = userId,
                IsApproved = false // Pending approval
            };
            var createdSpeaker = await _speakerRepository.CreateAsync(speaker);
            speakerId = createdSpeaker.Id;
        }

        topic.VolunteerSpeakerId = speakerId;
        await _topicRepository.UpdateAsync(topic);
        return true;
    }

    public async Task<bool> RemoveVolunteerAsync(int topicId, int userId)
    {
        var topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic == null || !topic.VolunteerSpeakerId.HasValue)
        {
            return false;
        }

        var speaker = await _speakerRepository.GetByIdAsync(topic.VolunteerSpeakerId.Value);
        if (speaker == null || speaker.UserId != userId)
        {
            return false; // Not the right user
        }

        topic.VolunteerSpeakerId = null;
        await _topicRepository.UpdateAsync(topic);
        return true;
    }

    public async Task<bool> IsUserVolunteerAsync(int topicId, int userId)
    {
        var topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic?.VolunteerSpeakerId == null)
        {
            return false;
        }

        var speaker = await _speakerRepository.GetByIdAsync(topic.VolunteerSpeakerId.Value);
        return speaker?.UserId == userId;
    }
}
