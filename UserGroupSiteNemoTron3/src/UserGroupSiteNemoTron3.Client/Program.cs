using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteNemoTron3.Client.Services;
using UserGroupSiteNemoTron3.Shared.Services;

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

// Register application services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITopicSuggestionService, TopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

await builder.Build().RunAsync();