namespace UserGroupSiteNemoTron3.Data.Models;

public class EventSpeaker : EntityBase
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsPrimary { get; set; }
}