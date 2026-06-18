namespace UserGroupSiteKimiK26.Shared.Dtos;

public class TopicSuggestionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int SuggestedByUserId { get; set; }
    public string SuggestedByName { get; set; } = "";
    public int? VolunteerUserId { get; set; }
    public string? VolunteerName { get; set; }
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
}

public class CreateSuggestionDto
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public class VoteResultDto
{
    public bool HasVoted { get; set; }
    public int VoteCount { get; set; }
}