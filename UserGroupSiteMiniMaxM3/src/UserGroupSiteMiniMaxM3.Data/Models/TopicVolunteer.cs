using System.ComponentModel.DataAnnotations.Schema;

namespace UserGroupSiteMiniMaxM3.Data.Models;

/// <summary>
/// One user's offer to present a topic. Composite key (TopicSuggestionId, UserId)
/// enforces "one volunteer per user per topic" at the database layer.
/// </summary>
public class TopicVolunteer
{
    public int TopicSuggestionId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TopicSuggestionId))]
    public TopicSuggestion? Topic { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}