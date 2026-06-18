namespace UserGroupSiteKimiK26.Shared.Dtos;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public List<int> SpeakerIds { get; set; } = [];
}

public class EventListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public List<SpeakerDto> Speakers { get; set; } = [];
}

public class SpeakerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
}