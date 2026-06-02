using UserGroupSiteMiniMaxM3.Data.Interfaces;
using UserGroupSiteMiniMaxM3.Shared.Models;

namespace UserGroupSiteMiniMaxM3.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="Shared.Services.IUserAdminService"/>. Wraps the
/// <see cref="Data.Services.IUserAdminService"/> and resolves the current user id
/// from the <see cref="IUserService"/> so the self-demotion guard works
/// identically whether the page is pre-rendered server-side or hydrated WASM.
/// </summary>
public class ServerUserAdminService(
    Data.Services.IUserAdminService inner,
    IUserService userService) : Shared.Services.IUserAdminService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
        => inner.ListUsersAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateRolesAsync(int targetUserId, bool isAdmin, bool isSpeaker, CancellationToken cancellationToken = default)
        => inner.UpdateRolesAsync(targetUserId, isAdmin, isSpeaker, userService.UserId, cancellationToken);
}