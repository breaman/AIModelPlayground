using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Data.Interfaces;

/// <summary>
/// Service for managing topic suggestions — creating, voting, and volunteering.
/// </summary>
public interface ITopicSuggestionService
{
    /// <summary>Gets all topic suggestions with their vote counts.</summary>
    Task<IEnumerable<TopicSuggestion>> GetAllTopicsAsync();

    /// <summary>Gets a topic suggestion by ID. Returns null if not found.</summary>
    Task<TopicSuggestion?> GetTopicByIdAsync(int id);

    /// <summary>Creates a new topic suggestion.</summary>
    Task<TopicSuggestion> CreateTopicAsync(TopicSuggestion topic);

    /// <summary>Records a vote by the given user on a topic. Enforces one-vote-per-user constraint.</summary>
    Task VoteAsync(int topicSuggestionId, int userId);

    /// <summary>Removes the given user's vote from a topic.</summary>
    Task UnvoteAsync(int topicSuggestionId, int userId);

    /// <summary>
    /// Sets the user as the volunteer for a topic. Only succeeds if no one has volunteered yet
    /// (first-come-first-served).
    /// </summary>
    Task VolunteerAsync(int topicSuggestionId, int userId);

    /// <summary>Removes the current user's volunteer assignment. Only the current volunteer can unvolunteer.</summary>
    Task UnvolunteerAsync(int topicSuggestionId, int userId);

    /// <summary>Checks whether the given user has already voted on a topic.</summary>
    Task<bool> HasVotedAsync(int topicSuggestionId, int userId);
}