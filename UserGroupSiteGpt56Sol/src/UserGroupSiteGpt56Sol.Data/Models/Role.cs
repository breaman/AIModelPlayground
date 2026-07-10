using UserGroupSiteGpt56Sol.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteGpt56Sol.Data.Models;

public class Role : IdentityRole<int>, IEntityBase
{
}