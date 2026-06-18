using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteGlm52.Client.Services;
using UserGroupSiteGlm52.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddScoped<IToastService, ToastService>();

// Dual-mode services: WebAssembly uses the HTTP-backed client implementations.
// (The Server project registers the DB-backed implementations used during pre-render.)
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicService, ClientTopicService>();
builder.Services.AddScoped<IUserAdminService, ClientUserAdminService>();

// Markdown rendering for the live preview; the same impl is used server-side for the public detail page.
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();

await builder.Build().RunAsync();