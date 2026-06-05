using UserGroupSiteNemoTron3.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteNemoTron3.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}