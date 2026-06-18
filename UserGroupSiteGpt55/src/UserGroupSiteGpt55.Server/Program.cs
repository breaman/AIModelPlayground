using System.Diagnostics;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteGpt55.Client.Services;
using UserGroupSiteGpt55.Data.Interfaces;
using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Server.Components;
using UserGroupSiteGpt55.Server.Components.Account;
using UserGroupSiteGpt55.Server.Components.Email;
using UserGroupSiteGpt55.Server.Endpoints;
using UserGroupSiteGpt55.Server.Services;
using UserGroupSiteGpt55.Server.Services.Events;
using UserGroupSiteGpt55.Server.Services.Topics;
using UserGroupSiteGpt55.Server.Services.Users;
using UserGroupSiteGpt55.ServiceDefaults;
using UserGroupSiteGpt55.Shared.Authorization;
using UserGroupSiteGpt55.Shared.Events;
using UserGroupSiteGpt55.Shared.Markdown;
using UserGroupSiteGpt55.Shared.Services;
using UserGroupSiteGpt55.Shared.Topics;
using UserGroupSiteGpt55.Shared.Users;

Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

var isMigrations = Environment.GetCommandLineArgs()[0].Contains("ef.dll");

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (!isMigrations)
    {
        builder.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration));
    }

    builder.AddServiceDefaults();

    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents()
        .AddAuthenticationStateSerialization();

    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityRedirectManager>();

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(ApplicationPolicies.AdminUsers, policy => policy.RequireRole(ApplicationRoles.Admin));
        options.AddPolicy(ApplicationPolicies.CreateEvents, policy => policy.RequireRole(ApplicationRoles.Admin));
        options.AddPolicy(ApplicationPolicies.EditEvents, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.Speaker));
        options.AddPolicy(ApplicationPolicies.ManageTopics, policy => policy.RequireAuthenticatedUser());
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString(Constants.DatabaseConnectionString))
            .EnableSensitiveDataLogging());
    builder.EnrichSqlServerDbContext<ApplicationDbContext>();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            // options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;

            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        // AddRoles isn't added from the AddIdentityCore, so if you want to use roles, this must be explicitly added
        .AddRoles<Role>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

    builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();
    builder.Services.AddScoped<IUserService, HttpUserService>();
    builder.Services.AddScoped<IToastService, ToastService>();
    builder.Services.AddScoped<IMarkdownRenderer, MarkdigMarkdownRenderer>();
    builder.Services.AddScoped<EventAuthorizationService>();
    builder.Services.AddScoped<IEventService, ServerEventService>();
    builder.Services.AddScoped<IUserAdminService, ServerUserAdminService>();
    builder.Services.AddScoped<ITopicSuggestionService, ServerTopicSuggestionService>();

    // Add route configuration to enforce lowercase URLs for better SEO
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    var app = builder.Build();

    await RoleSeeder.SeedAsync(app.Services);

    app.MapDefaultEndpoints();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    app.MapStaticAssets();

    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(UserGroupSiteGpt55.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();
    app.MapUserGroupEndpoints();

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "StopTheHostException" &&
                           ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Unhandled exception.");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}