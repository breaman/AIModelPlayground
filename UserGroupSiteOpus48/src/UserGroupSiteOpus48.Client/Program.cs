using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteOpus48.Client.Services;
using UserGroupSiteOpus48.Shared.Services;

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

// Dual-mode services: on the client these call the server's HTTP API (the Server project
// registers DB-backed implementations of the same interfaces for pre-render).
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicService, ClientTopicService>();
builder.Services.AddScoped<IUserAdminService, ClientUserAdminService>();

await builder.Build().RunAsync();