using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;

using UserGroupSiteQwen35.Data.Interfaces;

namespace UserGroupSiteQwen35.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(50)]
    public string? FirstName { get; set; }
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateTime MemberSince { get; set; }
}