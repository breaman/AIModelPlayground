using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteNemoTron3.Data.Models;

public class Event : FingerPrintEntityBase
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = null!;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public DateTime EventDateTime { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
}