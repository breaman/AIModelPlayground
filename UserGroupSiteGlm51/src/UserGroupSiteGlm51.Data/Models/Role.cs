using UserGroupSiteGlm51.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteGlm51.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}