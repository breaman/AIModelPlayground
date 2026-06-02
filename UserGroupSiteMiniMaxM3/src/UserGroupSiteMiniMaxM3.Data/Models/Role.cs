using Microsoft.AspNetCore.Identity;

using UserGroupSiteMiniMaxM3.Data.Interfaces;

namespace UserGroupSiteMiniMaxM3.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}