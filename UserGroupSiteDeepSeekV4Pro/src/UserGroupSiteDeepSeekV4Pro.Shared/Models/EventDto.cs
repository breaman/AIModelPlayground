namespace UserGroupSiteDeepSeekV4Pro.Shared.Models;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset EventDateTime { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public List<int> SpeakerUserIds { get; set; } = [];
    public List<UserDto> Speakers { get; set; } = [];
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
