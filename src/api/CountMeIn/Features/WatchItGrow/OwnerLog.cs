using CountMeIn.Domain;
using CountMeIn.Store;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.WatchItGrow;

/// <summary>The Owner Log view: every field except the key hash, for the holder of the owner key
/// only; anyone else gets null (the endpoint answers 403). The browser's How It's Going view is
/// worked out from this.</summary>
public static class OwnerLog
{
    public static IReadOnlyList<Dictionary<string, object?>>? Project(IReadOnlyList<object> history, string? ownerKey)
    {
        if (!Ids.OwnerKeyMatches(ownerKey, history.OfType<InterestCheckStarted>().FirstOrDefault()?.OwnerKeyHash)) return null;
        return history.Select(e =>
        {
            var flat = CheckStore.Flat(e);
            flat.Remove("ownerKeyHash");
            return flat;
        }).ToList();
    }
}

/// <summary>The store adapter for the Owner Log.</summary>
public static class GetOwnerEvents
{
    public static void Map(RouteGroupBuilder group) => group.MapGet("/checks/{checkId}/owner-events", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });

        var log = OwnerLog.Project(CheckStore.Unwrap(events), http.Headers[Ids.OwnerKeyHeader].ToString());
        if (log is null) return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);
        return Results.Ok(CheckStore.WithTimes(log, events));
    }
}
