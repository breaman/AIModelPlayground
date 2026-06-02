namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class EventSpeaker
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
