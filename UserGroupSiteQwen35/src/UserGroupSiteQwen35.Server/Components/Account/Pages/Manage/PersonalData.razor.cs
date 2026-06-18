using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Server.Components.Account.Pages.Manage;

public partial class PersonalData : ComponentBase
{
    [Inject] private UserManager<User> UserManager { get; set; } = default!;
    [Inject] private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
        }
    }
}