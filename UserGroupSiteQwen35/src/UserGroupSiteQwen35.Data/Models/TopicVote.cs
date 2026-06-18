using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteQwen35.Data.Models;

public class TopicVote : EntityBase
{
    [Required]
    public int TopicSuggestionId { get; set; }

    public TopicSuggestion? TopicSuggestion { get; set; }

    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime VotedOn { get; set; }
}