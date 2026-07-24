namespace UserGroupSiteGpt56Sol.Data.Models;

/// <summary>Records one user's vote for one topic.</summary>
public sealed class TopicVote
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedOnUtc { get; set; }
}