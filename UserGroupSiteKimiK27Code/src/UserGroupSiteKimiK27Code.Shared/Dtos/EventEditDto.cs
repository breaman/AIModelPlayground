using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK27Code.Shared.Dtos;

public class EventEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(200, ErrorMessage = "Slug cannot exceed 200 characters.")]
    public string Slug { get; set; } = "";

    [MaxLength(500, ErrorMessage = "Short description cannot exceed 500 characters.")]
    public string? ShortDescription { get; set; }

    public string Description { get; set; } = "";

    public DateTime EventDate { get; set; }

    [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public List<int> SpeakerIds { get; set; } = [];

    public bool CanPublish()
    {
        return !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Slug)
            && !string.IsNullOrWhiteSpace(Description)
            && EventDate > DateTime.MinValue
            && !string.IsNullOrWhiteSpace(Location)
            && SpeakerIds.Count > 0;
    }
}