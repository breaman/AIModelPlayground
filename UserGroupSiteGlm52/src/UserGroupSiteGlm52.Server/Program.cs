using System.Diagnostics;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteGlm52.Client.Services;
using UserGroupSiteGlm52.Data.Interfaces;
using UserGroupSiteGlm52.Data.Models;
using UserGroupSiteGlm52.Server.Components;
using UserGroupSiteGlm52.Server.Components.Account;
using UserGroupSiteGlm52.Server.Components.Email;
using UserGroupSiteGlm52.Server.Endpoints;
using UserGroupSiteGlm52.Server.Services;
using UserGroupSiteGlm52.ServiceDefaults;
using UserGroupSiteGlm52.Shared.Services;

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
        // Role-based policy used by Minimal API endpoints that require an administrator.
        options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
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

    // IHttpContextAccessor backs HttpUserService (reads the current user id from claims)
    // and the server services' role/identity checks.
    builder.Services.AddHttpContextAccessor();

    // Dual-mode services: the server implementations (DB-backed) are used during
    // pre-render and by static SSR pages; the client (HTTP) implementations are
    // registered in the Client project for WebAssembly hydration.
    builder.Services.AddScoped<IEventService, ServerEventService>();
    builder.Services.AddScoped<ITopicService, ServerTopicService>();
    builder.Services.AddScoped<IUserAdminService, ServerUserAdminService>();

    // Markdown rendering is stateless and used by both the server detail page and
    // (registered in the Client) the live preview.
    builder.Services.AddSingleton<IMarkdownService, MarkdownService>();

    // HTML sanitizer (server-side only) strips untrusted markup from rendered
    // Markdown before it is emitted as MarkupString on the public detail page.
    // Scoped to avoid sharing an instance across concurrent requests.
    builder.Services.AddScoped<Ganss.Xss.HtmlSanitizer>();

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
        .AddAdditionalAssemblies(typeof(UserGroupSiteGlm52.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();

    // Minimal API endpoints for events, topics, and admin user management.
    app.MapAppApi();

    // Aspire applies migrations before the server starts, so the schema is ready.
    // Skip seeding during EF tooling runs (no live database to seed against).
    if (!isMigrations)
    {
        await DataSeeder.SeedAsync(app.Services);
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