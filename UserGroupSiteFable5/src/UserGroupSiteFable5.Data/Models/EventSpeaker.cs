namespace UserGroupSiteFable5.Data.Models;

/// <summary>
/// Explicit many-to-many join between <see cref="Event"/> and <see cref="User"/>
/// (composite key) so speaker assignment is directly queryable for authorization checks.
/// </summary>
public class EventSpeaker
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}