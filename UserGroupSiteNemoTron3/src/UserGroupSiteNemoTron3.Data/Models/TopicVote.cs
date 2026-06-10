namespace UserGroupSiteNemoTron3.Data.Models;

public class TopicVote : EntityBase
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime VotedOn { get; set; } = DateTime.UtcNow;
}