using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserGroupSiteGpt56Sol.Data.Models;

/// <summary>Creates the application context for EF Core design-time migration commands.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UserGroupSiteGpt56Sol;Trusted_Connection=True")
            .Options;
        return new ApplicationDbContext(options);
    }
}