using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class Event : FingerPrintEntityBase
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Required, MaxLength(200)] public string Slug { get; set; } = "";
    [MaxLength(500)] public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset EventDateTime { get; set; }
    [MaxLength(500)] public string? Location { get; set; }
    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}