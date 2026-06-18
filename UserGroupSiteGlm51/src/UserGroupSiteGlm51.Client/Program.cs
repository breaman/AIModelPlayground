using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteGlm51.Client.Services;
using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

// Register client-side service implementations
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITopicSuggestionService, TopicSuggestionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

await builder.Build().RunAsync();