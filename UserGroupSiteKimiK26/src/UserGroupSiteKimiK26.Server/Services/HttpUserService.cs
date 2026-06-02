using System.Security.Claims;

using UserGroupSiteKimiK26.Data.Interfaces;

namespace UserGroupSiteKimiK26.Server.Services;

public class HttpUserService : IUserService
{
    private IHttpContextAccessor HttpContextAccessor { get; }
    public HttpUserService(IHttpContextAccessor httpContextAccessor)
    {
        HttpContextAccessor = httpContextAccessor;
    }
    public int UserId
    {
        get
        {
            return Convert.ToInt32(HttpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}