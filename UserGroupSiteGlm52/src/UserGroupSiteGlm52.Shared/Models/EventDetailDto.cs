namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>
/// Full public event detail. <see cref="Description"/> is the raw Markdown; the
/// caller renders it to HTML (sanitized on the server, raw preview in the editor).
/// </summary>
public sealed record EventDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public DateTime? EventDate { get; init; }
    public string? Location { get; init; }
    public bool IsPublished { get; init; }
    public IReadOnlyList<SpeakerDto> Speakers { get; init; } = [];
}