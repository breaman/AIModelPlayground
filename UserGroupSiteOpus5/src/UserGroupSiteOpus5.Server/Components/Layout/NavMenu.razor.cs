using UserGroupSiteOpus5.Shared.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserGroupSiteOpus5.Server.Components.Layout;

public partial class NavMenu : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private string FirstName { get; set; } = "";

    /// <summary>
    /// Roles that can reach the meeting management screen. Built from <see cref="RoleNames"/> so
    /// the navigation and the authorization policy cannot drift apart.
    /// </summary>
    private static string ManagerRoles => $"{RoleNames.Admin},{RoleNames.Speaker}";

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var firstNameClaim = user.FindFirst("FirstName");
                FirstName = firstNameClaim?.Value ?? user.Identity.Name ?? "";
            }
        }
    }
}
