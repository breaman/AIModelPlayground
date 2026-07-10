using UserGroupSiteGpt56Sol.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Data.Models;

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