namespace UserGroupSiteOpus48.Shared.Dtos;

/// <summary>
/// A user who can be assigned as a speaker. Used by the event editor's multi-select and the
/// event detail page's speaker list.
/// </summary>
public record SpeakerDto(int Id, string? FirstName, string? LastName, string? Email)
{
    /// <summary>Best available display name: full name if present, otherwise the email.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? Email ?? $"User {Id}"
            : $"{FirstName} {LastName}".Trim();
}