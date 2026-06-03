using System.ComponentModel.DataAnnotations;

using UserGroupSiteSonnet46.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteSonnet46.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(50)]
    public string? FirstName { get; set; }
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateTime MemberSince { get; set; }
}