namespace UserGroupSiteGpt56Sol.Data.Models;

/// <summary>Assigns an Identity user as a speaker for an event.</summary>
public sealed class EventSpeaker
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}