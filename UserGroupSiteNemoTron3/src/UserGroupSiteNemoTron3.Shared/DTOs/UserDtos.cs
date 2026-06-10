namespace UserGroupSiteNemoTron3.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime MemberSince { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class UpdateUserRolesDto
{
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }
}