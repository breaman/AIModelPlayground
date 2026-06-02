using UserGroupSiteMiniMaxM3.Data.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteMiniMaxM3.Data.Models;

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