using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.WatchItGrow;

public sealed record OwnerProgressView(string Title, int Count, int Goal, int ToGo, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, int MeterBoxes, int MeterOn);

/// <summary>Owner Progress: how far the check has come. Owner only.</summary>
public static class OwnerProgress
{
    public static OwnerProgressView Project(IReadOnlyList<object> history, string today)
    {
        string title = ""; var goal = 0; string? closesOn = null; var byOwner = false;
        var isIn = new HashSet<string>();
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted s: title = s.Title; goal = s.Goal; closesOn = s.ClosesOn; break;
                case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                case InterestShown s: isIn.Add(s.PersonId); break;
                case InterestWithdrawn w: isIn.Remove(w.PersonId); break;
                case InterestCheckClosed: byOwner = true; break;
            }
        var count = isIn.Count;
        var closed = byOwner || (closesOn is not null && string.CompareOrdinal(closesOn, today) < 0);
        var boxes = Math.Min(goal, 10);
        var on = goal <= 0 ? 0 : Math.Min(boxes, (int)Math.Round((double)count / goal * boxes, MidpointRounding.AwayFromZero));
        if (goal <= 0) boxes = Math.Max(boxes, 0);
        return new(title, count, goal, Math.Max(0, goal - count), closed ? "closed" : count >= goal ? "reached" : "open", closesOn, boxes, on);
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/owner-progress", Handle);

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
