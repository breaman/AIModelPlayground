namespace UserGroupSiteOpus48.Shared.Dtos;

/// <summary>A user row for the admin user-management page, with current role membership.</summary>
public record UserAdminDto(
    int Id,
    string? FirstName,
    string? LastName,
    string? Email,
    bool IsAdmin,
    bool IsSpeaker);

/// <summary>Payload to grant or revoke a single role for a single user.</summary>
public record SetRoleRequest(int UserId, string Role, bool InRole);