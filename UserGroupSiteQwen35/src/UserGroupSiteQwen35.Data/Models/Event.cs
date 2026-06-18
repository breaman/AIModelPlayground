using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteQwen35.Data.Models;

public class Event : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime DateTime { get; set; }

    public string? Location { get; set; }

    public bool IsPublished { get; set; } = false;
}