using UserGroupSiteFable5.Client.Services;
using UserGroupSiteFable5.Shared.Services;

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
builder.Services.AddScoped<IEventService, EventApiClient>();
builder.Services.AddScoped<IUserAdminService, UserApiClient>();
builder.Services.AddScoped<ITopicService, TopicApiClient>();

await builder.Build().RunAsync();