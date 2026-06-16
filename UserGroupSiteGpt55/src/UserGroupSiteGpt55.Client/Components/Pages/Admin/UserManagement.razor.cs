using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt55.Shared.Users;

namespace UserGroupSiteGpt55.Client.Components.Pages.Admin;

public partial class UserManagement : ComponentBase
{
    [Inject]
    private IUserAdminService UserAdminService { get; set; } = default!;

    protected List<UserAdminListItem>? Users { get; set; }

    protected string? Message { get; set; }

    protected bool MessageIsError { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Users = (await UserAdminService.GetUsersAsync()).ToList();
    }

    protected async Task SaveAsync(UserAdminListItem user)
    {
        var result = await UserAdminService.UpdateRolesAsync(new UserRoleUpdateRequest(user.Id, user.IsAdmin, user.IsSpeaker));
        MessageIsError = !result.Succeeded;
        Message = result.Succeeded ? "Roles updated." : string.Join(" ", result.Errors);
        Users = (await UserAdminService.GetUsersAsync()).ToList();
    }

    protected void SetAdmin(UserAdminListItem user, ChangeEventArgs args)
    {
        if (Users is null || user.IsCurrentUser)
        {
            return;
        }

        ReplaceUser(user with { IsAdmin = IsChecked(args) });
    }

    protected void SetSpeaker(UserAdminListItem user, ChangeEventArgs args)
    {
        ReplaceUser(user with { IsSpeaker = IsChecked(args) });
    }

    protected static string GetName(UserAdminListItem user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "(No name)" : name;
    }

    private void ReplaceUser(UserAdminListItem updated)
    {
        if (Users is null)
        {
            return;
        }

        var index = Users.FindIndex(u => u.Id == updated.Id);
        if (index >= 0)
        {
            Users[index] = updated;
        }
    }

    private static bool IsChecked(ChangeEventArgs args)
    {
        return args.Value is true || args.Value?.ToString() == "true";
    }
}
