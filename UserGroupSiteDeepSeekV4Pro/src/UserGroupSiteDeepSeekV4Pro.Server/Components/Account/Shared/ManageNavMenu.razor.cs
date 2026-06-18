using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

using UserGroupSiteDeepSeekV4Pro.Data.Models;

namespace UserGroupSiteDeepSeekV4Pro.Server.Components.Account.Shared;

public partial class ManageNavMenu : ComponentBase
{
    [Inject] private SignInManager<User> SignInManager { get; set; } = default!;

    private bool _hasExternalLogins;

    protected override async Task OnInitializedAsync()
    {
        _hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any();
    }
}