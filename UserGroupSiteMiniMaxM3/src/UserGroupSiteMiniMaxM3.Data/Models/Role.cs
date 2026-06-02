using UserGroupSiteMiniMaxM3.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteMiniMaxM3.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}