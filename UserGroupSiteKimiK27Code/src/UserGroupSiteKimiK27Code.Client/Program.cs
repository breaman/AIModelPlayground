using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteKimiK27Code.Client.Services;
using UserGroupSiteKimiK27Code.Shared.Services;

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

await builder.Build().RunAsync();