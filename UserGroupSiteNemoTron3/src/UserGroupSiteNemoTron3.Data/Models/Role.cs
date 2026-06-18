using Microsoft.AspNetCore.Identity;

using UserGroupSiteNemoTron3.Data.Interfaces;

namespace UserGroupSiteNemoTron3.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}