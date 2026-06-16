using System.ComponentModel.DataAnnotations;

using UserGroupSiteGpt55.Shared.Topics;

namespace UserGroupSiteGpt55.Data.Models.Topics;

/// <summary>
/// Represents a talk topic suggested by an authenticated user.
/// </summary>
public sealed class TopicSuggestion : EntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(2000)]
    public string Description { get; set; } = "";

    public int SuggestedByUserId { get; set; }

    public User SuggestedByUser { get; set; } = null!;

    public int? VolunteerSpeakerUserId { get; set; }

    public User? VolunteerSpeakerUser { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public TopicSuggestionStatus Status { get; set; }

    public List<TopicVote> Votes { get; set; } = [];
}
