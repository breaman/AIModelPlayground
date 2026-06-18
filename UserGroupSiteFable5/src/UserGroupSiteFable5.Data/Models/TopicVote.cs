namespace UserGroupSiteFable5.Data.Models;

/// <summary>
/// A single user's vote for a topic. The composite primary key
/// (<see cref="TopicSuggestionId"/>, <see cref="UserId"/>) enforces one vote
/// per user per topic at the database level.
/// </summary>
public class TopicVote
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}