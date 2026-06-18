namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>
/// Lightweight event summary used in lists (home page, manage list).
/// Carries only data the Client is allowed to see.
/// </summary>
public sealed record EventSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? ShortDescription { get; init; }
    public DateTime? EventDate { get; init; }
    public string? Location { get; init; }
    public bool IsPublished { get; init; }
    public IReadOnlyList<string> SpeakerNames { get; init; } = [];
}