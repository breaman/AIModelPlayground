using System.Security.Claims;

using UserGroupSiteGpt56Sol.Shared.Authorization;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Reads the authenticated user from the current HTTP request.</summary>
public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
{
    public ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    public int UserId => int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public bool IsAdmin => Principal.IsInRole(AppRoles.Admin);
}