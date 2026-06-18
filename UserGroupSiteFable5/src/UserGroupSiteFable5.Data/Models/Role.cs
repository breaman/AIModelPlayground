using Microsoft.AspNetCore.Identity;

using UserGroupSiteFable5.Data.Interfaces;

namespace UserGroupSiteFable5.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}