using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteDeepSeekV4Pro.Client.Services;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

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
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<ITopicSuggestionService, ClientTopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, ClientUserManagementService>();

await builder.Build().RunAsync();