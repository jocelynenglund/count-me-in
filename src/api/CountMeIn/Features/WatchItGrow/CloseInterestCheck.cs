using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>OwnerKey is what the caller sent (the raw key); the stream holds only its hash.</summary>
public sealed record CloseInterestCheck(string CheckId, string? OwnerKey, string Today);

public static class CloseInterestCheckSlice
{
    public static Decision Decide(IReadOnlyList<object> history, CloseInterestCheck cmd)
    {
        var check = CheckState.From(history);
        if (!check.Started) return Decision.NotFound("No such interest check");
        if (!Streams.OwnerKeyMatches(cmd.OwnerKey, check.OwnerKeyHash)) return Decision.Forbidden("Only the owner can close this");
        if (check.Closed) return Decision.Conflict("It's already closed");

        return Decision.Accept(new InterestCheckClosed(cmd.CheckId, cmd.Today));
    }
}

public static class CloseInterestCheckEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/close", Handle);

    // No body parameter on purpose: the owner key rides in a header, so an empty POST is fine.
    private static async Task<IResult> Handle(string checkId, HttpRequest http, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var cmd = new CloseInterestCheck(checkId, http.Headers[Streams.OwnerKeyHeader].ToString(), Streams.Today());
        var d = await CheckStore.Execute(store, checkId, history => CloseInterestCheckSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
