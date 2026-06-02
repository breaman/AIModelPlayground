namespace UserGroupSiteGlm51.Data.Models;

/// <summary>
/// Join entity linking an Event to a User who is speaking at that event.
/// Each user can be a speaker on multiple events, and each event can have multiple speakers.
/// </summary>
public class EventSpeaker : EntityBase
{
    public int EventId { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the event.
    /// </summary>
    public Event Event { get; set; } = null!;

    /// <summary>
    /// Navigation property to the speaker (User).
    /// </summary>
    public User User { get; set; } = null!;
}