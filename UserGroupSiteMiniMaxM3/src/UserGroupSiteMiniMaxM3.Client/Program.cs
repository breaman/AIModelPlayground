using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteMiniMaxM3.Client.Services;
using UserGroupSiteMiniMaxM3.Shared.Services;

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
builder.Services.AddScoped<IUserAdminService, ClientUserAdminService>();
builder.Services.AddScoped<ITopicService, ClientTopicService>();

await builder.Build().RunAsync();