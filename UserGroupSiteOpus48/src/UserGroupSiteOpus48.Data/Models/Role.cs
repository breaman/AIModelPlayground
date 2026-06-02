using UserGroupSiteOpus48.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteOpus48.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}