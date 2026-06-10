namespace UserGroupSiteNemoTron3.Shared.DTOs;

public class TopicSuggestionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? VolunteerSpeakerId { get; set; }
    public string? VolunteerSpeakerName { get; set; }
    public int VoteCount { get; set; }
    public bool HasCurrentUserVoted { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class CreateTopicSuggestionDto
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}