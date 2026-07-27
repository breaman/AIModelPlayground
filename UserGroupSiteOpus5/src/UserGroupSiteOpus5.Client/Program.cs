using UserGroupSiteOpus5.Client.Services;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// The handler attaches the anti-CSRF header the API requires on state-changing calls, so no
// individual service can forget it.
builder.Services.AddTransient<ClientRequestHeaderHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ClientRequestHeaderHandler>();
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
});

builder.Services.AddScoped<IToastService, ToastService>();

// Client-side halves of the dual-service pattern. The Server project registers database-backed
// implementations of the same interfaces, which run during pre-rendering.
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicSuggestionService, ClientTopicSuggestionService>();
builder.Services.AddScoped<IUserAdminService, ClientUserAdminService>();

await builder.Build().RunAsync();
