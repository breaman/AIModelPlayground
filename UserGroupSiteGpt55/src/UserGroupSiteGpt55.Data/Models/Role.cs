using Microsoft.AspNetCore.Identity;

using UserGroupSiteGpt55.Data.Interfaces;

namespace UserGroupSiteGpt55.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}