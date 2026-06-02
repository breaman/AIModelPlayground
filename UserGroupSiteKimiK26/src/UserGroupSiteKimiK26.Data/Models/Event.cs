using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK26.Data.Models;

public class Event : FingerPrintEntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(200)]
    public string Slug { get; set; } = "";

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public DateTime? EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}
