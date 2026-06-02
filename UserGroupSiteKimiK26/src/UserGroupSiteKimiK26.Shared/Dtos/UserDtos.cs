namespace UserGroupSiteKimiK26.Shared.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserRolesDto
{
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }
}
