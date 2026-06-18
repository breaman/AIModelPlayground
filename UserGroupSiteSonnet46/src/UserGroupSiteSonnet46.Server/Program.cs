using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteSonnet46.Client.Services;
using UserGroupSiteSonnet46.Data.Interfaces;
using UserGroupSiteSonnet46.Data.Models;
using UserGroupSiteSonnet46.Server.Components;
using UserGroupSiteSonnet46.Server.Components.Account;
using UserGroupSiteSonnet46.Server.Components.Email;
using UserGroupSiteSonnet46.Server.Services;
using UserGroupSiteSonnet46.ServiceDefaults;
using UserGroupSiteSonnet46.Shared.Services;

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
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
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

    // Feature services — server implementations used for SSR pre-rendering and API handlers
    builder.Services.AddScoped<IUserManagementService, ServerUserManagementService>();
    builder.Services.AddScoped<IEventService, ServerEventService>();
    builder.Services.AddScoped<ITopicService, ServerTopicService>();

    // Add route configuration to enforce lowercase URLs for better SEO
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    var app = builder.Build();

    // Seed roles and any startup data
    await SeedRolesAsync(app);

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

    // --- API Endpoints ---

    MapUserApiEndpoints(app);
    MapEventApiEndpoints(app);
    MapTopicApiEndpoints(app);

    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(UserGroupSiteSonnet46.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();

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

// ---------------------------------------------------------------------------
// Role seeding
// ---------------------------------------------------------------------------

static async Task SeedRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    foreach (var roleName in new[] { "Admin", "Speaker" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new Role { Name = roleName });
        }
    }
}

// ---------------------------------------------------------------------------
// API: Users (/api/users)
// ---------------------------------------------------------------------------

static void MapUserApiEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/users").RequireAuthorization("AdminOnly");

    group.MapGet("/", async (IUserManagementService service) =>
        Results.Ok(await service.GetAllUsersAsync()));

    group.MapPost("/{userId:int}/roles", async (
        int userId,
        RoleChangeRequest request,
        IUserManagementService service,
        ClaimsPrincipal caller) =>
    {
        // Prevent an admin from removing their own Admin role
        var callerIdStr = caller.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(callerIdStr, out var callerId)
            && callerId == userId
            && request.Role == "Admin"
            && !request.Enabled)
        {
            return Results.BadRequest("You cannot remove your own Admin role.");
        }

        await service.SetRoleAsync(userId, request.Role, request.Enabled);
        return Results.Ok();
    });
}

// ---------------------------------------------------------------------------
// API: Events (/api/events)
// ---------------------------------------------------------------------------

static void MapEventApiEndpoints(WebApplication app)
{
    // Public endpoints
    app.MapGet("/api/events/published", async (IEventService service) =>
        Results.Ok(await service.GetPublishedEventsAsync()));

    app.MapGet("/api/events/by-slug/{slug}", async (string slug, IEventService service) =>
    {
        var ev = await service.GetEventBySlugAsync(slug);
        return ev is null ? Results.NotFound() : Results.Ok(ev);
    });

    // Authenticated endpoints
    var auth = app.MapGroup("/api/events").RequireAuthorization();

    auth.MapGet("/", async (IEventService service) =>
        Results.Ok(await service.GetAllEventsAsync()));

    auth.MapGet("/{id:int}", async (int id, IEventService service) =>
    {
        var ev = await service.GetEventByIdAsync(id);
        return ev is null ? Results.NotFound() : Results.Ok(ev);
    });

    auth.MapPost("/", async (EventSaveDto dto, IEventService service) =>
        Results.Ok(await service.SaveEventAsync(dto)));

    auth.MapPut("/{id:int}", async (int id, EventSaveDto dto, IEventService service) =>
    {
        var updated = dto with { Id = id };
        return Results.Ok(await service.SaveEventAsync(updated));
    });

    // Admin-only: speaker list for the assignment dropdown
    auth.MapGet("/speakers", async (IEventService service) =>
        Results.Ok(await service.GetSpeakersAsync()))
        .RequireAuthorization("AdminOnly");
}

// ---------------------------------------------------------------------------
// API: Topics (/api/topics)
// ---------------------------------------------------------------------------

static void MapTopicApiEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/topics").RequireAuthorization();

    group.MapGet("/", async (ITopicService service) =>
        Results.Ok(await service.GetTopicsAsync()));

    group.MapPost("/", async (TopicRequest request, ITopicService service) =>
        Results.Ok(await service.SuggestTopicAsync(request.Title, request.Description)));

    group.MapPost("/{id:int}/vote", async (int id, ITopicService service) =>
    {
        await service.VoteAsync(id);
        return Results.Ok();
    });

    group.MapPost("/{id:int}/volunteer", async (int id, ITopicService service) =>
    {
        try
        {
            await service.VolunteerAsync(id);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    });
}

// ---------------------------------------------------------------------------
// Local request types
// ---------------------------------------------------------------------------

/// <summary>Body for POST /api/users/{userId}/roles.</summary>
record RoleChangeRequest(string Role, bool Enabled);

/// <summary>Body for POST /api/topics.</summary>
record TopicRequest(string Title, string? Description);