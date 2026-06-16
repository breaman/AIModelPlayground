namespace UserGroupSiteGpt55.Data.Models.Topics;

/// <summary>
/// Represents a single authenticated user's vote for a suggested topic.
/// </summary>
public sealed class TopicVote
{
    public int TopicSuggestionId { get; set; }

    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset CreatedOn { get; set; }
}
