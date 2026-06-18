using Microsoft.AspNetCore.Identity;

using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}