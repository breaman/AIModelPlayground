namespace UserGroupSiteSonnet46.Data.Models;

/// <summary>
/// One vote per user per topic. Composite PK (TopicSuggestionId, UserId) enforces the uniqueness constraint.
/// </summary>
public class TopicVote
{
    public int TopicSuggestionId { get; set; }

    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}