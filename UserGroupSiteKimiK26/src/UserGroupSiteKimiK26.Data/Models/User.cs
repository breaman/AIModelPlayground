using System.ComponentModel.DataAnnotations;

using UserGroupSiteKimiK26.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteKimiK26.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(50)]
    public string? FirstName { get; set; }
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateTime MemberSince { get; set; }
}