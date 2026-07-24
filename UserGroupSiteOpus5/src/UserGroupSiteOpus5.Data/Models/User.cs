using System.ComponentModel.DataAnnotations;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Data.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteOpus5.Data.Models;

public class User : IdentityUser<int>, IEntityBase
{
    [MaxLength(FieldLengths.PersonName)]
    public string? FirstName { get; set; }
    [MaxLength(FieldLengths.PersonName)]
    public string? LastName { get; set; }
    public DateTimeOffset MemberSince { get; set; }
}