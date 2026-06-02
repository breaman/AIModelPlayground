using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class ApplicationDbContext : AuthDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserService userService) :
        base(options, userService)
    {
    }
}