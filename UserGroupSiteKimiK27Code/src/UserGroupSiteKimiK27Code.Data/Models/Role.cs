using Microsoft.AspNetCore.Identity;

using UserGroupSiteKimiK27Code.Data.Interfaces;

namespace UserGroupSiteKimiK27Code.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}