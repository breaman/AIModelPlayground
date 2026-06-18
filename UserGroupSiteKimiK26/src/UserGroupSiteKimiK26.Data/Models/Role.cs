using Microsoft.AspNetCore.Identity;

using UserGroupSiteKimiK26.Data.Interfaces;

namespace UserGroupSiteKimiK26.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}