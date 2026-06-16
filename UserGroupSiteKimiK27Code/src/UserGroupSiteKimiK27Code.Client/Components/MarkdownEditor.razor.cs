using System.Net.Http.Json;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteKimiK27Code.Client.Components;

public partial class MarkdownEditor : ComponentBase
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Parameter]
    public string Markdown { get; set; } = "";

    [Parameter]
    public EventCallback<string> MarkdownChanged { get; set; }

    private string _activeTab = "edit";
    private string? _previewHtml;
    private bool _isPreviewLoading;
    private readonly string _textareaId = $"md-editor-{Guid.NewGuid():N}";

    private async Task OnMarkdownInput(ChangeEventArgs e)
    {
        Markdown = e.Value?.ToString() ?? "";
        if (MarkdownChanged.HasDelegate)
        {
            await MarkdownChanged.InvokeAsync(Markdown);
        }
    }

    private async Task SetTabAsync(string tab)
    {
        _activeTab = tab;
        if (tab == "preview")
        {
            await RefreshPreviewAsync();
        }
    }

    private async Task RefreshPreviewAsync()
    {
        _isPreviewLoading = true;
        try
        {
            var response = await HttpClient.PostAsJsonAsync("api/markdown/preview", new { Markdown });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PreviewResponse>();
                _previewHtml = result?.Html;
            }
            else
            {
                _previewHtml = "<p class='text-danger'>Unable to render preview.</p>";
            }
        }
        finally
        {
            _isPreviewLoading = false;
        }
    }

    private sealed class PreviewResponse
    {
        public string Html { get; set; } = "";
    }
}