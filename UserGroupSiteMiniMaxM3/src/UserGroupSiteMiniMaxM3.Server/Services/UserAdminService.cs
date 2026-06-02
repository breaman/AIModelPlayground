using Microsoft.AspNetCore.Identity;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models;

namespace UserGroupSiteMiniMaxM3.Server.Services;

/// <inheritdoc cref="IUserAdminService"/>
public class UserAdminService(UserManager<User> userManager) : Data.Services.IUserAdminService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = userManager.Users.ToList();
        var summaries = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user).WaitAsync(cancellationToken);
            summaries.Add(new UserSummary(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email,
                user.FirstName,
                user.LastName,
                user.MemberSince,
                roles.ToList()));
        }
        return summaries;
    }

    /// <inheritdoc />
    public async Task UpdateRolesAsync(int targetUserId, bool isAdmin, bool isSpeaker, int currentUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Self-demotion guard: an admin cannot remove their own Admin role while signed in.
        if (targetUserId == currentUserId && !isAdmin)
        {
            throw new InvalidOperationException("You cannot remove your own Admin role while signed in as an admin.");
        }

        var user = await userManager.FindByIdAsync(targetUserId.ToString()).WaitAsync(cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with id {targetUserId} not found.");
        }

        // Apply Admin role
        var isCurrentlyAdmin = await userManager.IsInRoleAsync(user, Roles.Admin).WaitAsync(cancellationToken);
        if (isAdmin && !isCurrentlyAdmin)
        {
            var add = await userManager.AddToRoleAsync(user, Roles.Admin).WaitAsync(cancellationToken);
            if (!add.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add Admin role: {string.Join(", ", add.Errors.Select(e => e.Description))}");
            }
        }
        else if (!isAdmin && isCurrentlyAdmin)
        {
            var remove = await userManager.RemoveFromRoleAsync(user, Roles.Admin).WaitAsync(cancellationToken);
            if (!remove.Succeeded)
            {
                throw new InvalidOperationException($"Failed to remove Admin role: {string.Join(", ", remove.Errors.Select(e => e.Description))}");
            }
        }

        // Apply Speaker role
        var isCurrentlySpeaker = await userManager.IsInRoleAsync(user, Roles.Speaker).WaitAsync(cancellationToken);
        if (isSpeaker && !isCurrentlySpeaker)
        {
            var add = await userManager.AddToRoleAsync(user, Roles.Speaker).WaitAsync(cancellationToken);
            if (!add.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add Speaker role: {string.Join(", ", add.Errors.Select(e => e.Description))}");
            }
        }
        else if (!isSpeaker && isCurrentlySpeaker)
        {
            var remove = await userManager.RemoveFromRoleAsync(user, Roles.Speaker).WaitAsync(cancellationToken);
            if (!remove.Succeeded)
            {
                throw new InvalidOperationException($"Failed to remove Speaker role: {string.Join(", ", remove.Errors.Select(e => e.Description))}");
            }
        }
    }
}