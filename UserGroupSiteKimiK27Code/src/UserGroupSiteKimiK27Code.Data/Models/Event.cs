using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK27Code.Data.Models;

public class Event : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = "";

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string Description { get; set; } = "";

    public DateTime EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<EventSpeaker> EventSpeakers { get; set; } = [];
}