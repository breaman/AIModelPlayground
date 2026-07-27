using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace UserGroupSiteOpus5.Server.Components.Layout;

/// <summary>
/// The application shell: navigation, the routed page, and the toast host.
/// </summary>
public partial class MainLayout : LayoutComponentBase
{
    private ErrorBoundary? _errorBoundary;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Navigating away from a faulted page should clear the error rather than carry it to the
        // next route.
        _errorBoundary?.Recover();
    }

    /// <summary>Clears the error so the failed content is re-rendered.</summary>
    private Task RecoverAsync()
    {
        _errorBoundary?.Recover();
        return Task.CompletedTask;
    }
}
