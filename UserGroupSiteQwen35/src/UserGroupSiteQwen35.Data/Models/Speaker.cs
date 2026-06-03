using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteQwen35.Data.Models;

public class Speaker : EntityBase
{
    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }

    public string? Bio { get; set; }

    public bool IsApproved { get; set; } = false;
}
