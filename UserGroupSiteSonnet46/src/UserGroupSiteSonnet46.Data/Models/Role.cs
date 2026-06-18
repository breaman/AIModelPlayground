using Microsoft.AspNetCore.Identity;

using UserGroupSiteSonnet46.Data.Interfaces;

namespace UserGroupSiteSonnet46.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}