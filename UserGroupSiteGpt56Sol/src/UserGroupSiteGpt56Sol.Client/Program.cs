using UserGroupSiteGpt56Sol.Client.Services;
using UserGroupSiteGpt56Sol.Shared.Services;

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
builder.Services.AddScoped<AntiforgeryHttpClient>();
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicService, ClientTopicService>();
builder.Services.AddScoped<IUserAdministrationService, ClientUserAdministrationService>();

await builder.Build().RunAsync();