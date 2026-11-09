using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.ShowInterest;

public sealed record MyPlaceView(string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

/// <summary>My Place: where this visitor stands: can join, in (by the name they gave) or closed. Closed wins.</summary>
public static class MyPlace
{
    public static MyPlaceView Project(IReadOnlyList<object> history, string? personId, string today)
    {
        if (Closed(history, today)) return new("closed", null);
        var me = personId is null ? null : PeopleIn(history).FirstOrDefault(p => p.PersonId == personId);
        return me is null ? new("can-join", null) : new("in", me.Name);
    }

    /// <summary>Closed by the owner, or by a closing date before today (the latest date wins).</summary>
    private static bool Closed(IReadOnlyList<object> history, string today)
    {
        string? closesOn = null;
        var byOwner = false;
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted s: closesOn = s.ClosesOn; break;
                case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                case InterestCheckClosed: byOwner = true; break;
            }
        return byOwner || (closesOn is not null && string.CompareOrdinal(closesOn, today) < 0);
    }

    /// <summary>This view's fold: everyone who is in now, in the order they first joined, with their latest name and email.</summary>
    private static List<InterestShown> PeopleIn(IReadOnlyList<object> history)
    {
        var order = new List<string>();
        var latest = new Dictionary<string, InterestShown>();
        var isIn = new HashSet<string>();
        foreach (var e in history)
            switch (e)
            {
                case InterestShown s:
                    if (!latest.ContainsKey(s.PersonId)) order.Add(s.PersonId);
                    latest[s.PersonId] = s; isIn.Add(s.PersonId); break;
                case InterestWithdrawn w: isIn.Remove(w.PersonId); break;
            }
        return order.Where(isIn.Contains).Select(id => latest[id]).ToList();
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/my-place", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        var person = http.Headers[Ids.PersonIdHeader].ToString();
        return Results.Ok(Project(history, Ids.IsValidId(person) ? person : null, Ids.Today()));
    }
}
