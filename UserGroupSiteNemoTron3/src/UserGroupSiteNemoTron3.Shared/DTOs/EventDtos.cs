namespace UserGroupSiteNemoTron3.Shared.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime EventDateTime { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public EventSpeakerDto[] Speakers { get; set; } = [];
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
}

public class EventSpeakerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public bool IsPrimary { get; set; }
}

public class CreateEventDto
{
    public string Title { get; set; } = "";
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime EventDateTime { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public int[] SpeakerUserIds { get; set; } = [];
}

public class UpdateEventDto
{
    public string Title { get; set; } = "";
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public DateTime EventDateTime { get; set; }
    public string? Location { get; set; }
    public bool IsPublished { get; set; }
    public int[] SpeakerUserIds { get; set; } = [];
}