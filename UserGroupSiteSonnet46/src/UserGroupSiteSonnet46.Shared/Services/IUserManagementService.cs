namespace UserGroupSiteSonnet46.Shared.Services;

/// <summary>Represents a user in the system for display and role management.</summary>
public record UserDto(int Id, string? FirstName, string? LastName, string? Email, bool IsAdmin, bool IsSpeaker);

/// <summary>
/// Service for listing users and toggling Admin/Speaker role membership.
/// </summary>
public interface IUserManagementService
{
    /// <summary>Returns all registered users with their current role assignments.</summary>
    Task<List<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Adds or removes <paramref name="role"/> for the specified user.
    /// </summary>
    /// <param name="userId">The target user's primary key.</param>
    /// <param name="role">Role name, e.g. "Admin" or "Speaker".</param>
    /// <param name="enabled">True to add the role; false to remove it.</param>
    Task SetRoleAsync(int userId, string role, bool enabled);
}
