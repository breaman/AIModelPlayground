using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using UserGroupSiteGpt56Sol.Server.Endpoints;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class EndpointSecurityTests
{
    [Fact]
    public void EveryApplicationWriteRequiresAuthorizationAndAntiforgery()
    {
        var endpoints = BuildEndpoints();
        var writes = endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
            .Any(method => !HttpMethods.IsGet(method)) == true).ToList();

        Assert.NotEmpty(writes);
        Assert.All(writes, endpoint =>
        {
            Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
            Assert.True(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        });
    }

    [Fact]
    public void PublicEventQueriesAreAnonymousWhileAdminRoutesRequireAdminRole()
    {
        var endpoints = BuildEndpoints();
        var publicList = endpoints.Single(endpoint => endpoint.RoutePattern.RawText == "/api/events/");
        var adminUsers = endpoints.Where(endpoint =>
            endpoint.RoutePattern.RawText?.StartsWith("/api/admin/users", StringComparison.Ordinal) == true);

        Assert.Empty(publicList.Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.All(adminUsers, endpoint =>
        {
            var declaredRoles = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .SelectMany(data => data.Roles?.Split(',') ?? []);
            var policyRoles = endpoint.Metadata.GetMetadata<AuthorizationPolicy>()?.Requirements
                .OfType<RolesAuthorizationRequirement>().SelectMany(requirement => requirement.AllowedRoles) ?? [];
            Assert.Contains(AppRoles.Admin, declaredRoles.Concat(policyRoles), StringComparer.Ordinal);
        });
    }

    /// <summary>Builds application route metadata without starting a network listener.</summary>
    private static IReadOnlyList<RouteEndpoint> BuildEndpoints()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery();
        builder.Services.AddScoped<IEventService>(_ => null!);
        builder.Services.AddScoped<ITopicService>(_ => null!);
        builder.Services.AddScoped<IUserAdministrationService>(_ => null!);
        var app = builder.Build();
        app.MapApplicationEndpoints();
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().ToList();
    }
}
