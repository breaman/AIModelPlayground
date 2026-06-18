namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>Minimal speaker representation for pickers and lists.</summary>
public sealed record SpeakerDto
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
}

/// <summary>
/// A speaker option in the event editor's multi-select, with a flag indicating
/// whether they are already assigned to the event being edited.
/// </summary>
public sealed record SpeakerOptionDto
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
    public bool IsAssigned { get; init; }
}