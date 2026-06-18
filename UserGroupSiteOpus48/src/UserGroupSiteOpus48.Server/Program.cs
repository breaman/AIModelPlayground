using System.Diagnostics;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteOpus48.Client.Services;
using UserGroupSiteOpus48.Data.Interfaces;
using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Server.Components;
using UserGroupSiteOpus48.Server.Components.Account;
using UserGroupSiteOpus48.Server.Components.Email;
using UserGroupSiteOpus48.Server.Endpoints;
using UserGroupSiteOpus48.Server.Services;
using UserGroupSiteOpus48.ServiceDefaults;
using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Services;

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
        // Admin-only operations (user management, event creation).
        options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(RoleNames.Admin));

        // Event editing entry points: any admin or speaker (per-event check happens in the service).
        options.AddPolicy(Policies.EventEditors,
            policy => policy.RequireRole(RoleNames.Admin, RoleNames.Speaker));
    });

    // Server data services need the current user's ClaimsPrincipal for role checks.
    builder.Services.AddHttpContextAccessor();

    // RFC 9457 problem-details responses for unhandled API errors.
    builder.Services.AddProblemDetails();

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

    // Dual-mode services: the Server implementations hit the database directly and are used by the
    // API endpoints and during Blazor pre-render (the Client registers HTTP-backed versions).
    builder.Services.AddScoped<IEventService, ServerEventService>();
    builder.Services.AddScoped<ITopicService, ServerTopicService>();
    builder.Services.AddScoped<IUserAdminService, ServerUserAdminService>();

    // Bind bootstrap admin configuration used by DbSeeder.
    builder.Services.Configure<BootstrapAdminOptions>(
        builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

    // Add route configuration to enforce lowercase URLs for better SEO
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    var app = builder.Build();

    // Seed roles and the bootstrap admin on startup (migrations are applied by Aspire beforehand).
    // Skipped during EF design-time tooling (ef.dll) which only needs the model.
    if (!isMigrations)
    {
        await DbSeeder.SeedAsync(app.Services);
    }

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
        .AddAdditionalAssemblies(typeof(UserGroupSiteOpus48.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();

    // Application API endpoints consumed by the WebAssembly client.
    app.MapEventEndpoints();
    app.MapTopicEndpoints();
    app.MapUserAdminEndpoints();

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