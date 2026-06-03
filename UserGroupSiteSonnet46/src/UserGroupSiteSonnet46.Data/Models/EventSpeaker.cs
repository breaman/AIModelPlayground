namespace UserGroupSiteSonnet46.Data.Models;

/// <summary>
/// Join entity for the many-to-many relationship between <see cref="Event"/> and <see cref="User"/> speakers.
/// Composite PK (EventId, UserId) is configured in <see cref="ApplicationDbContext"/>.
/// </summary>
public class EventSpeaker
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}
