namespace UserGroupSiteGlm52.Data.Models;

/// <summary>
/// Join entity for the many-to-many relationship between an <see cref="Event"/>
/// and a <see cref="User"/> speaking at it. An assigned speaker may edit the event.
/// </summary>
public class EventSpeaker : FingerPrintEntityBase
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}