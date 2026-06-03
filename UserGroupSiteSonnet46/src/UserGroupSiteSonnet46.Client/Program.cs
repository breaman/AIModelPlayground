using UserGroupSiteSonnet46.Client.Services;
using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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
builder.Services.AddScoped<IUserManagementService, ClientUserManagementService>();
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicService, ClientTopicService>();

await builder.Build().RunAsync();
