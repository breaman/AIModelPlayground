using UserGroupSiteFable5.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteFable5.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}