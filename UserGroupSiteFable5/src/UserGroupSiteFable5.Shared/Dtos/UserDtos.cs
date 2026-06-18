using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Shared.Dtos;

/// <summary>A user row in the admin role-management table.</summary>
public class UserAdminDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace($"{FirstName} {LastName}".Trim())
        ? Email
        : $"{FirstName} {LastName}".Trim();
}

/// <summary>Role assignment payload for PUT /api/users/{id}/roles.</summary>
public class UpdateUserRolesDto
{
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }
}

/// <summary>A user in the Speaker role, for the event speaker picker.</summary>
public class SpeakerDto
{
    public int Id { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;
}