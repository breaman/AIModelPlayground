using System.Diagnostics;

using UserGroupSiteOpus5.Client.Services;
using UserGroupSiteOpus5.Data.Interfaces;
using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Server.Authorization;
using UserGroupSiteOpus5.Server.Components;
using UserGroupSiteOpus5.Server.Components.Account;
using UserGroupSiteOpus5.Server.Components.Email;
using UserGroupSiteOpus5.Server.Data;
using UserGroupSiteOpus5.Server.Endpoints;
using UserGroupSiteOpus5.Server.Services;
using UserGroupSiteOpus5.ServiceDefaults;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

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

    // The WebAssembly client calls /api carrying the Identity cookie. The cookie handler's default
    // challenge is a 302 to the login page, which a JSON caller cannot act on — it would try to
    // parse an HTML login page as its response body. Return status codes on API paths and keep the
    // redirect for ordinary page navigation.
    builder.Services.ConfigureApplicationCookie(options =>
    {
        var defaultRedirectToLogin = options.Events.OnRedirectToLogin;
        var defaultRedirectToAccessDenied = options.Events.OnRedirectToAccessDenied;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            return defaultRedirectToLogin(context);
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            return defaultRedirectToAccessDenied(context);
        };
    });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(RoleNames.Admin))
        .AddPolicy(PolicyNames.SpeakerOrAdmin, policy => policy.RequireRole(RoleNames.Admin, RoleNames.Speaker))
        // Resource-based: satisfied by any admin, or by a speaker assigned to the event whose id is
        // passed as the resource. See EventEditorAuthorizationHandler.
        .AddPolicy(PolicyNames.EventEditor, policy => policy.AddRequirements(new EventEditRequirement()));

    builder.Services.AddScoped<IAuthorizationHandler, EventEditorAuthorizationHandler>();

    // Role claims are carried in the auth cookie, so a role change does not take effect until the
    // cookie is revalidated. A short interval keeps the lag between an admin granting a role and
    // the user seeing it down to minutes rather than a full sign-out.
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.ValidationInterval = builder.Environment.IsDevelopment()
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromMinutes(30);
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
    // HttpUserService and the server-side feature services read the current principal from the
    // ambient HttpContext, so the accessor is registered explicitly rather than relied upon as a
    // side effect of another AddX call.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserService, HttpUserService>();
    builder.Services.AddScoped<IToastService, ToastService>();

    // Server-side halves of the dual-service pattern. These run during pre-rendering and behind
    // the API endpoints; the Client project registers HTTP-calling implementations of the same
    // interfaces for the hydrated WebAssembly components.
    builder.Services.AddScoped<IEventService, ServerEventService>();
    builder.Services.AddScoped<ITopicSuggestionService, ServerTopicSuggestionService>();
    builder.Services.AddScoped<IUserAdminService, ServerUserAdminService>();

    // Shapes unhandled failures as RFC 9457 problem details so API errors are consistent.
    builder.Services.AddProblemDetails();

    builder.Services.AddHostedService<DatabaseSeeder>();

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

    // Re-execute status codes into the not-found page for browser navigation only. Applying it to
    // /api as well corrupts API responses: a 401 on a POST gets re-executed against the Blazor
    // not-found endpoint, whose antiforgery check then rewrites it into an empty 400, so the
    // client cannot tell "sign in" from "bad request".
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    app.MapStaticAssets();

    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(UserGroupSiteOpus5.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();

    app.MapEventEndpoints();
    app.MapTopicSuggestionEndpoints();
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

/// <summary>
/// Names the implicit entry-point class so <c>WebApplicationFactory&lt;Program&gt;</c> can boot the
/// real application in integration tests. Top-level statements otherwise generate an internal type
/// that the test host cannot reference.
/// </summary>
public partial class Program;