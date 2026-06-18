using Microsoft.AspNetCore.Identity;

using UserGroupSiteOpus48.Data.Interfaces;

namespace UserGroupSiteOpus48.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}