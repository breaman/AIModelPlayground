namespace UserGroupSiteGpt55.Data.Models.Events;

/// <summary>
/// Links events to users assigned as speakers or editors.
/// </summary>
public sealed class EventSpeaker
{
    public int EventId { get; set; }

    public UserGroupEvent Event { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}
