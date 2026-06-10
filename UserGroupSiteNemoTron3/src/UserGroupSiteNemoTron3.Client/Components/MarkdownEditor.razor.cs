using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UserGroupSiteNemoTron3.Client.Components;

public partial class MarkdownEditor : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public string Value { get; set; } = "";

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    public string ActiveTab { get; set; } = "edit";
    private IJSObjectReference? _markedModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _markedModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./marked.esm.js");
        }
    }

    private async Task RenderPreview()
    {
        if (_markedModule != null && !string.IsNullOrWhiteSpace(Value))
        {
            var html = await _markedModule.InvokeAsync<string>("parse", Value);
            // Note: In a real implementation, you'd want to sanitize the HTML
            // For now, we'll use the JS interop to set the innerHTML
            await JSRuntime.InvokeVoidAsync("setMarkdownPreview", html);
        }
    }

    private async Task OnValueChanged(string newValue)
    {
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
    }

    public async ValueTask DisposeAsync()
    {
        if (_markedModule != null)
        {
            await _markedModule.DisposeAsync();
        }
    }
}