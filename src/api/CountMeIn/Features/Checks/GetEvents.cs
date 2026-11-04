using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>The public log: flat events with every private field removed (no name, email, personId or key hash).</summary>
public static class GetEvents
{
    public static void Map(RouteGroupBuilder group) => group.MapGet("/checks/{checkId}/events", Handle);

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        return Results.Ok(events.Select(CheckStore.ToPublic));
    }
}
