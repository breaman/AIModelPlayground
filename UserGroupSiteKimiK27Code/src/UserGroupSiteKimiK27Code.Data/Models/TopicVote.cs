namespace UserGroupSiteKimiK27Code.Data.Models;

public class TopicVote : EntityBase
{
    public int TopicSuggestionId { get; set; }
    public TopicSuggestion TopicSuggestion { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;
}