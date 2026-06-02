namespace UserGroupSiteDeepSeekV4Pro.Shared.Models;

public class TopicSuggestionListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public UserDto? SuggestedByUser { get; set; }
    public UserDto? VolunteerUser { get; set; }
    public int VoteCount { get; set; }
    public bool CurrentUserHasVoted { get; set; }
    public bool CurrentUserIsVolunteer { get; set; }
    public DateTime? CreatedOn { get; set; }
}
