using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using UserGroupSiteGpt55.Client.Services;
using UserGroupSiteGpt55.Client.Services.Events;
using UserGroupSiteGpt55.Client.Services.Markdown;
using UserGroupSiteGpt55.Client.Services.Topics;
using UserGroupSiteGpt55.Client.Services.Users;
using UserGroupSiteGpt55.Shared.Events;
using UserGroupSiteGpt55.Shared.Markdown;
using UserGroupSiteGpt55.Shared.Services;
using UserGroupSiteGpt55.Shared.Topics;
using UserGroupSiteGpt55.Shared.Users;

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
builder.Services.AddScoped<IMarkdownRenderer, ClientMarkdownRenderer>();
builder.Services.AddScoped<IEventService, ClientEventService>();
builder.Services.AddScoped<IUserAdminService, ClientUserAdminService>();
builder.Services.AddScoped<ITopicSuggestionService, ClientTopicSuggestionService>();

await builder.Build().RunAsync();