using System.Diagnostics;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using UserGroupSiteQwen35.Client.Services;
using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;
using UserGroupSiteQwen35.Data.Repositories;
using UserGroupSiteQwen35.Server.Authorization;
using UserGroupSiteQwen35.Server.Components;
using UserGroupSiteQwen35.Server.Components.Account;
using UserGroupSiteQwen35.Server.Components.Email;
using UserGroupSiteQwen35.Server.Endpoints;
using UserGroupSiteQwen35.Server.Services;
using UserGroupSiteQwen35.ServiceDefaults;
using UserGroupSiteQwen35.Shared.Services;

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
        options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Admin));
        options.AddPolicy("SpeakerOrAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Admin) || context.User.IsInRole(RoleNames.Speaker)));
        options.AddPolicy("EventEditor", policy =>
            policy.AddRequirements(new UserGroupSiteQwen35.Server.Authorization.EventEditorRequirement()));
    });

    builder.Services.AddScoped<AuthorizationHandler<EventEditorRequirement>, EventEditorHandler>();

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

    // Repositories
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<ISpeakerRepository, SpeakerRepository>();
    builder.Services.AddScoped<ITopicSuggestionRepository, TopicSuggestionRepository>();
    builder.Services.AddScoped<ITopicVoteRepository, TopicVoteRepository>();

    // Services
    builder.Services.AddScoped<UserManagementService>();
    builder.Services.AddScoped<EventService>();
    builder.Services.AddScoped<TopicSuggestionService>();

    // Client services
    builder.Services.AddHttpClient<IUserManagementClientService, UserManagementClientService>(client =>
        client.BaseAddress = new Uri("https://localhost"));
    builder.Services.AddHttpClient<IEventClientService, EventClientService>(client =>
        client.BaseAddress = new Uri("https://localhost"));
    builder.Services.AddHttpClient<ITopicSuggestionClientService, TopicSuggestionClientService>(client =>
        client.BaseAddress = new Uri("https://localhost"));

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
        .AddAdditionalAssemblies(typeof(UserGroupSiteQwen35.Client._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();

    // Map API endpoints
    app.MapUserManagementEndpoints();
    app.MapEventEndpoints();
    app.MapTopicSuggestionEndpoints();

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