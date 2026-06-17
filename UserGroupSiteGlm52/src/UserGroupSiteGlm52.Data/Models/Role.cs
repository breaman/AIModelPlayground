using Microsoft.AspNetCore.Identity;

using UserGroupSiteGlm52.Data.Interfaces;

namespace UserGroupSiteGlm52.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}