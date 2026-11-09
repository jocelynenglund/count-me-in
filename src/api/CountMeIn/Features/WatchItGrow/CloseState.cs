using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.WatchItGrow;

public sealed record CloseStateView(string Status);

/// <summary>Close State: open, closed by the owner or closed by its date. Owner only.</summary>
public static class CloseState
{
    public static CloseStateView Project(IReadOnlyList<object> history, string today)
    {
        string? closesOn = null; var byOwner = false;
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted s: closesOn = s.ClosesOn; break;
                case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                case InterestCheckClosed: byOwner = true; break;
            }
        var byDate = closesOn is not null && string.CompareOrdinal(closesOn, today) < 0;
        return new(byOwner ? "closed-by-owner" : byDate ? "closed-by-date" : "open");
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/close-state", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        var owner = history.OfType<InterestCheckStarted>().FirstOrDefault()?.OwnerKeyHash;
        if (!Ids.OwnerKeyMatches(http.Headers[Ids.OwnerKeyHeader].ToString(), owner))
            return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);
        return Results.Ok(Project(history, Ids.Today()));
    }
}
