using UserGroupSiteSonnet46.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteSonnet46.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}