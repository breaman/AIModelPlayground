namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class TopicVote
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
