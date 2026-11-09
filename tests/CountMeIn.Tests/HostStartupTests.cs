using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
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
            endpoints.Count.ShouldBe(23); // nine commands, reads and Version, plus 14 view endpoints
            endpoints.ShouldAllBe(e => e.RoutePattern.RawText!.StartsWith(CountMeInModule.Prefix));

            // With GWT_EXPORT, also leave the module's route list beside the specs: the web's screens pass checks
            // what the pages call against it, and the build book's usedBy is matched to views and commands by it.
            // "type" is the class that owns the endpoint (PitchCard, ChooseThemeEndpoint), which is how a route
            // finds its view or command in the export (a slice's type there is the same class, or X + "Slice").
            if (Environment.GetEnvironmentVariable("GWT_EXPORT") is { Length: > 0 } dir)
            {
                Directory.CreateDirectory(dir);
                var routes = endpoints
                    .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).Select(m => new
                    {
                        method = m,
                        route = e.RoutePattern.RawText![CountMeInModule.Prefix.Length..],
                        type = OwnerOf(e.Metadata.GetMetadata<System.Reflection.MethodInfo>()),
                    }))
                    .OrderBy(r => r.route).ThenBy(r => r.method).ToList();
                File.WriteAllText(Path.Combine(dir, "routes.json"), System.Text.Json.JsonSerializer.Serialize(routes, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
        }
        finally { data.Delete(recursive: true); }
    }

    /// <summary>The top-level class a handler lives in (a lambda's compiler-made class is nested in it).</summary>
    static string OwnerOf(System.Reflection.MethodInfo? handler)
    {
        var t = handler?.DeclaringType;
        while (t?.DeclaringType is not null) t = t.DeclaringType;
        return t?.Name ?? "";
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
