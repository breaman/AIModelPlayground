namespace UserGroupSiteKimiK26.Data.Models;

public class EventSpeaker : FingerPrintEntityBase
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}