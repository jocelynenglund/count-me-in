using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CountMeIn.Tests;

/// <summary>
/// The module's endpoints, mapped into a real ASP.NET app the way the shared host maps them,
/// and the route table forced to build. Mistakes minimal APIs only report at startup (an
/// inferred body on a GET or DELETE, a parameter it can't bind) throw here instead of taking
/// down the shared host, and every app on it, at deploy time.
/// </summary>
public class HostStartupTests
{
    [Fact]
    public void Every_endpoint_maps_and_the_route_table_builds()
    {
        var data = Directory.CreateTempSubdirectory("countmein-startup-");
        try
        {
            var builder = WebApplication.CreateBuilder();
            var store = new FolderEventStore(data.FullName);
            builder.Services.AddSingleton<IEventStore>(store);
            builder.Services.AddSingleton<ILockingEventStore>(store);
            var module = new CountMeInModule();
            module.ConfigureServices(builder.Services, builder.Configuration);
            var app = builder.Build();

            module.MapEndpoints(app);

            // Building the endpoints is where the host would throw at startup.
            var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>().ToList();
            endpoints.Count.ShouldBe(6);
            endpoints.ShouldAllBe(e => e.RoutePattern.RawText!.StartsWith(CountMeInModule.Prefix));
        }
        finally { data.Delete(recursive: true); }
    }

    [Fact]
    public void The_module_is_public_and_named_for_storage()
    {
        var m = new CountMeInModule();
        m.RequiresApiKey.ShouldBeFalse();
        m.RoutePrefix.ShouldBe("/api/countmein");
        CountMeInModule.Module.ShouldBe("countmein");
    }
}
