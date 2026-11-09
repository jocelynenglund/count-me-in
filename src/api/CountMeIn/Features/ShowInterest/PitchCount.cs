using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.ShowInterest;

public sealed record PitchCountView(int Count, int Goal, bool Open, int MeterBoxes, int MeterOn);

/// <summary>Pitch Count: how many are in, against the goal, and the meter.</summary>
public static class PitchCount
{
    public static PitchCountView Project(IReadOnlyList<object> history, string today)
    {
        var f = Fold(history);
        var closed = f.ClosedByOwner || (f.ClosesOn is not null && string.CompareOrdinal(f.ClosesOn, today) < 0);
        var boxes = Math.Min(f.Goal, 10);
        if (f.Goal <= 0) return new(f.Count, f.Goal, !closed, Math.Max(boxes, 0), 0);
        var on = Math.Min(boxes, (int)Math.Round((double)f.Count / f.Goal * boxes, MidpointRounding.AwayFromZero));
        return new(f.Count, f.Goal, !closed, boxes, on);
    }

    /// <summary>This view's fold: the goal, the closing date, whether the owner closed it, and how many are in.</summary>
    private sealed record Folded(int Goal, string? ClosesOn, bool ClosedByOwner, int Count);

    private static Folded Fold(IReadOnlyList<object> history)
    {
        var goal = 0; string? closesOn = null; var byOwner = false;
        var isIn = new HashSet<string>();
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted s: goal = s.Goal; closesOn = s.ClosesOn; break;
                case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                case InterestShown s: isIn.Add(s.PersonId); break;
                case InterestWithdrawn w: isIn.Remove(w.PersonId); break;
                case InterestCheckClosed: byOwner = true; break;
            }
        return new(goal, closesOn, byOwner, isIn.Count);
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/pitch-count", Handle);

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        return Results.Ok(Project(history, Ids.Today()));
    }
}
