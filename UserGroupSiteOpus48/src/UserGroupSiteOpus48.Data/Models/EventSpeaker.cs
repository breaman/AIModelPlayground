namespace UserGroupSiteOpus48.Data.Models;

/// <summary>
/// Join entity linking an <see cref="Event"/> to a speaker (<see cref="User"/>).
/// A composite unique index on (EventId, UserId) prevents assigning the same speaker twice.
/// </summary>
public class EventSpeaker : FingerPrintEntityBase
{
    /// <summary>FK to the event.</summary>
    public int EventId { get; set; }

    /// <summary>The event this assignment belongs to.</summary>
    public Event Event { get; set; } = null!;

    /// <summary>FK to the speaker user.</summary>
    public int UserId { get; set; }

    /// <summary>The assigned speaker.</summary>
    public User User { get; set; } = null!;
}
