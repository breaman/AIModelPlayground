namespace UserGroupSiteGlm52.Data.Models;

/// <summary>
/// A single vote on a topic suggestion. The (TopicSuggestionId, UserId) pair is
/// uniquely indexed so each user may vote on a topic at most once.
/// </summary>
public class TopicVote : EntityBase
{
    public int TopicSuggestionId { get; set; }

    public TopicSuggestion Topic { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime VotedOn { get; set; }
}