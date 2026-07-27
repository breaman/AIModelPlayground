namespace UserGroupSiteOpus5.Data.Models;

/// <summary>
/// Assigns a user as a speaker for an event.
/// </summary>
/// <remarks>
/// Uses the inherited surrogate key rather than a composite (<c>EventId</c>, <c>UserId</c>) key so
/// that it stays compatible with <see cref="FingerPrintEntityBase"/> and the audit-log machinery,
/// both of which assume a single integer <c>Id</c>. Uniqueness of the pair is enforced by an index.
/// </remarks>
public class EventSpeaker : FingerPrintEntityBase
{
    /// <summary>The event being spoken at.</summary>
    public int EventId { get; set; }

    /// <summary>Navigation to the event.</summary>
    public Event Event { get; set; } = null!;

    /// <summary>The speaking user.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation to the user.</summary>
    public User User { get; set; } = null!;
}
