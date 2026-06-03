using UserGroupSiteQwen35.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteQwen35.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}