using UserGroupSiteGpt56Sol.Shared.Models;

namespace UserGroupSiteGpt56Sol.Shared.Services;

/// <summary>Provides administrator-only user role operations.</summary>
public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateRolesAsync(int userId, UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default);
}