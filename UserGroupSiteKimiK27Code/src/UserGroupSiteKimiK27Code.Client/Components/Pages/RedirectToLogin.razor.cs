using Microsoft.AspNetCore.Components;

namespace UserGroupSiteKimiK27Code.Client.Pages;

public partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationManager.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", forceLoad: true);
    }
}