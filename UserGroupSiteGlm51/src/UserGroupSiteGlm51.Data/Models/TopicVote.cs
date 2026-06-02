namespace UserGroupSiteGlm51.Data.Models;

/// <summary>
/// Represents a single vote by a user on a topic suggestion.
/// Each user can vote on each topic only once (enforced by unique composite index).
/// </summary>
public class TopicVote : EntityBase
{
    public int TopicSuggestionId { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the topic being voted on.
    /// </summary>
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who cast the vote.
    /// </summary>
    public User User { get; set; } = null!;
}