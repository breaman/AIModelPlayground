namespace UserGroupSiteGlm51.Shared.Models;

/// <summary>
/// DTO for returning user data to admin clients.
/// </summary>
public record UserDto(
    int Id,
    string? FirstName,
    string? LastName,
    string? Email,
    List<string> Roles
);

/// <summary>
/// DTO for returning a user's role assignments.
/// </summary>
public record UserRoleDto(
    int UserId,
    List<string> Roles
);