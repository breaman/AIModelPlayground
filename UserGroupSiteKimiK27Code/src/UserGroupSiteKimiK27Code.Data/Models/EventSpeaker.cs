namespace UserGroupSiteKimiK27Code.Data.Models;

public class EventSpeaker : EntityBase
{
    public int EventId { get; set; }
    public Event Event { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;
}