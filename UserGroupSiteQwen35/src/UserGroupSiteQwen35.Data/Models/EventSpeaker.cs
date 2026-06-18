using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteQwen35.Data.Models;

public class EventSpeaker
{
    [Required]
    public int EventId { get; set; }

    public Event? Event { get; set; }

    [Required]
    public int SpeakerId { get; set; }

    public Speaker? Speaker { get; set; }
}