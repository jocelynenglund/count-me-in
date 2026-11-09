using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.ShowInterest;

public sealed record PitchCardView(string Title, string Pitch, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, bool ClosedByDate);

/// <summary>Pitch Card: the pitch itself and whether it is open, reached or closed. No names, no emails.</summary>
public static class PitchCard
{
    public static PitchCardView Project(IReadOnlyList<object> history, string today)
    {
        var f = Fold(history);
        var closedByDate = !f.ClosedByOwner && f.ClosesOn is not null && string.CompareOrdinal(f.ClosesOn, today) < 0;
        var status = f.ClosedByOwner || closedByDate ? "closed" : f.Count >= f.Goal ? "reached" : "open";
        return new(f.Title, f.Pitch, status, f.ClosesOn, closedByDate);
    }

    /// <summary>This view's fold: the pitch, the goal, the closing date, whether the owner closed it, and how many are in.</summary>
    private sealed record Folded(string Title, string Pitch, int Goal, string? ClosesOn, bool ClosedByOwner, int Count);

    private static Folded Fold(IReadOnlyList<object> history)
    {
        string title = "", pitch = ""; int goal = 0; string? closesOn = null; var byOwner = false;
        var isIn = new HashSet<string>();
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted s: title = s.Title; pitch = s.Pitch; goal = s.Goal; closesOn = s.ClosesOn; break;
                case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                case InterestShown s: isIn.Add(s.PersonId); break;
                case InterestWithdrawn w: isIn.Remove(w.PersonId); break;
                case InterestCheckClosed: byOwner = true; break;
            }
        return new(title, pitch, goal, closesOn, byOwner, isIn.Count);
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/pitch-card", Handle);

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        return Results.Ok(Project(history, Ids.Today()));
    }
}
