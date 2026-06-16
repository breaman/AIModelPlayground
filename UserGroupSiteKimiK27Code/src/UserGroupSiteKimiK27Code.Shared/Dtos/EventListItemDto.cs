namespace UserGroupSiteKimiK27Code.Shared.Dtos;

public class EventListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public List<SpeakerDto> Speakers { get; set; } = [];
}