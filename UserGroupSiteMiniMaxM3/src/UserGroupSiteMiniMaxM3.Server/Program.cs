using System.Diagnostics;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteMiniMaxM3.Client.Services;
using UserGroupSiteMiniMaxM3.Data.Interfaces;
using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Server.Components;
using UserGroupSiteMiniMaxM3.Server.Components.Account;
using UserGroupSiteMiniMaxM3.Server.Components.Email;
using UserGroupSiteMiniMaxM3.Server.Endpoints;
using UserGroupSiteMiniMaxM3.Server.Services;
using UserGroupSiteMiniMaxM3.ServiceDefaults;
using UserGroupSiteMiniMaxM3.Shared.Services;

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
    builder.Services.AddAuthorization();

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
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserService, HttpUserService>();
    builder.Services.AddScoped<IToastService, ToastService>();

    // User admin (server-side, called from server-rendered or WASM components).
    // Data.Services.IUserAdminService is the EF-aware contract (with self-demotion guard that needs current user id).
    // Shared.Services.IUserAdminService is the cross-platform contract used by the page via the dual-mode pattern.
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Data.Services.IUserAdminService, UserAdminService>();
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Shared.Services.IUserAdminService, ServerUserAdminService>();

    // Event management
    builder.Services.AddScoped<EventValidator>();
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Data.Services.IEventService, UserGroupSiteMiniMaxM3.Data.Services.EventService>();
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Data.Services.IUserLookupService, UserGroupSiteMiniMaxM3.Data.Services.UserLookupService>();

    // Topic suggestions
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Data.Services.TopicService>();
    builder.Services.AddScoped<UserGroupSiteMiniMaxM3.Shared.Services.ITopicService, ServerTopicService>();

    // Add route configuration to enforce lowercase URLs for better SEO
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    var app = builder.Build();

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
        .AddAdditionalAssemblies(typeof(UserGroupSiteMiniMaxM3.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();
    app.MapUserAdminEndpoints();
    app.MapEventEndpoints();
    app.MapUserLookupEndpoints();
    app.MapTopicEndpoints();

    // Seed Identity roles (Admin, Speaker). Idempotent.
    if (!isMigrations)
    {
        await RoleSeeder.SeedAsync(app.Services);
    }

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