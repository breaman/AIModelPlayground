using UserGroupSiteKimiK26.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteKimiK26.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}