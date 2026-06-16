using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Interfaces;

namespace UserGroupSiteKimiK27Code.Data.Models;

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