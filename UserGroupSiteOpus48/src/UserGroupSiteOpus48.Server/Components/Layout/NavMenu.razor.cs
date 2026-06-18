using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteOpus48.Shared.Authorization;

namespace UserGroupSiteOpus48.Server.Components.Layout;

public partial class NavMenu : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private string FirstName { get; set; } = "";

    /// <summary>Comma-separated roles allowed to reach the event-editing areas.</summary>
    private static string EditorRoles => $"{RoleNames.Admin},{RoleNames.Speaker}";

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