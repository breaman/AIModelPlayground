using UserGroupSiteOpus5.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteOpus5.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}