using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public static class CloseCheck
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/close", Handle);

    // No body parameter on purpose: the owner key rides in a header, so an empty POST is fine.
    private static async Task<IResult> Handle(string checkId, HttpRequest http, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var key = http.Headers[Streams.OwnerKeyHeader].ToString();

        var d = await CheckStore.Run(store, checkId, state =>
        {
            if (!state.Started) return LegacyDecision.Refuse(404, "No such interest check");
            if (!Streams.OwnerKeyMatches(key, state.OwnerKeyHash)) return LegacyDecision.Refuse(403, "Only the owner can close this");
            if (state.Closed) return LegacyDecision.Refuse(409, "It's already closed");
            return LegacyDecision.Accept(new { },
                ("InterestCheckClosed", new() { ["checkId"] = checkId, ["on"] = Streams.Today() }));
        });
        return d.ToResult();
    }
}
