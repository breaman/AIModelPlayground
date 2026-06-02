using System.ComponentModel.DataAnnotations.Schema;

namespace UserGroupSiteMiniMaxM3.Data.Models;

/// <summary>
/// One user's vote on a topic. Composite key (TopicSuggestionId, UserId)
/// enforces "one vote per user per topic" at the database layer.
/// </summary>
public class TopicVote
{
    public int TopicSuggestionId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TopicSuggestionId))]
    public TopicSuggestion? Topic { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}