using Microsoft.AspNetCore.Identity;

using UserGroupSiteGlm51.Data.Interfaces;

namespace UserGroupSiteGlm51.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}