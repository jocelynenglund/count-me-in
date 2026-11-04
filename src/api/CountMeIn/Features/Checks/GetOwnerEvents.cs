using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>The owner's log: every field except the key hash, shown only to the holder of the owner key.</summary>
public static class GetOwnerEvents
{
    public static void Map(RouteGroupBuilder group) => group.MapGet("/checks/{checkId}/owner-events", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });

        var state = CheckState.Fold(events);
        if (!Streams.OwnerKeyMatches(http.Headers[Streams.OwnerKeyHeader].ToString(), state.OwnerKeyHash))
            return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);

        return Results.Ok(events.Select(CheckStore.ToOwner));
    }
}
