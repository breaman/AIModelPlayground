using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;

using UserGroupSiteSonnet46.Data.Interfaces;

namespace UserGroupSiteSonnet46.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(50)]
    public string? FirstName { get; set; }
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateTime MemberSince { get; set; }
}