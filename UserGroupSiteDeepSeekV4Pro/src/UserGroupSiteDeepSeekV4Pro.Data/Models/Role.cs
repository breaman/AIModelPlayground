using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}