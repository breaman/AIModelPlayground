using System.ComponentModel.DataAnnotations;

using UserGroupSiteGpt56Sol.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteGpt56Sol.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(50)]
    public string? FirstName { get; set; }
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateTime MemberSince { get; set; }

    public ICollection<EventSpeaker> SpeakingEvents { get; set; } = [];
    public ICollection<TopicSuggestion> CreatedTopicSuggestions { get; set; } = [];
    public ICollection<TopicSuggestion> VolunteeredTopicSuggestions { get; set; } = [];
    public ICollection<TopicVote> TopicVotes { get; set; } = [];
}