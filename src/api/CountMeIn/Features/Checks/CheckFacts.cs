using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>What the views read off one check's history: a shared fold, so every view agrees on "closed", "who's in" and the closing date.</summary>
public sealed class CheckFacts
{
    public bool Started { get; private set; }
    public string Title { get; private set; } = "";
    public string Pitch { get; private set; } = "";
    public int Goal { get; private set; }
    public string? OwnerKeyHash { get; private set; }
    public string Theme { get; private set; } = "classic";
    public string? ClosesOn { get; private set; }
    public bool ClosedByOwner { get; private set; }
    /// <summary>Everyone who is in, in first-insertion order, with the name and email of their latest InterestShown.</summary>
    public IReadOnlyList<InterestShown> People => _order.Where(_in.Contains).Select(id => _latest[id]).ToList();
    public IReadOnlyList<InterestShown> AllShown => _shown;
    private readonly HashSet<string> _in = [];
    private readonly List<string> _order = [];
    private readonly List<InterestShown> _shown = [];
    private readonly Dictionary<string, InterestShown> _latest = new();

    public int Count => _in.Count;

    public bool ClosedByDate(string today) => !ClosedByOwner && ClosesOn is not null && string.CompareOrdinal(ClosesOn, today) < 0;
    public bool Closed(string today) => ClosedByOwner || ClosedByDate(today);

    public static CheckFacts From(IReadOnlyList<object> history)
    {
        var f = new CheckFacts();
        // Someone who leaves and comes back keeps their first place, so keep our own order list.
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted st:
                    f.Started = true; f.Title = st.Title; f.Pitch = st.Pitch; f.Goal = st.Goal; f.ClosesOn = st.ClosesOn; f.OwnerKeyHash = st.OwnerKeyHash; break;
                case InterestShown sh:
                    f._shown.Add(sh);
                    if (!f._latest.ContainsKey(sh.PersonId)) f._order.Add(sh.PersonId);
                    f._latest[sh.PersonId] = sh;
                    f._in.Add(sh.PersonId);
                    break;
                case InterestWithdrawn w: f._in.Remove(w.PersonId); break;
                case InterestCheckClosed: f.ClosedByOwner = true; break;
                case ThemeChosen t: f.Theme = t.Theme; break;
                case ClosingDateChanged c: f.ClosesOn = c.ClosesOn; break;
            }
        return f;
    }

    public (int Boxes, int On) Meter()
    {
        var boxes = Math.Min(Goal, 10);
        if (Goal <= 0) return (Math.Max(boxes, 0), 0);
        var on = Math.Min(boxes, (int)Math.Round((double)Count / Goal * boxes, MidpointRounding.AwayFromZero));
        return (boxes, on);
    }

    public string Status(string today) => Closed(today) ? "closed" : Count >= Goal ? "reached" : "open";
}

/// <summary>The store adapters shared by the view endpoints.</summary>
public static class ViewRoutes
{
    private static async Task<(IResult? Error, IReadOnlyList<object> History)> Load(string checkId, IEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return (Results.BadRequest(new { error = "invalid checkId" }), []);
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return (Results.NotFound(new { error = "No such interest check" }), []);
        return (null, CheckStore.Unwrap(events));
    }

    /// <summary>GET /checks/{checkId}/views/{name}: anyone with the link.</summary>
    public static void Public(RouteGroupBuilder group, string name, Func<IReadOnlyList<object>, string, object> project) =>
        group.MapGet($"/checks/{{checkId}}/views/{name}", async (string checkId, IEventStore store) =>
        {
            var (err, history) = await Load(checkId, store);
            return err ?? Results.Ok(project(history, Streams.Today()));
        });

    /// <summary>Like <see cref="Public"/>, but the view also needs the caller's person id (header X-Person-Id; missing or invalid means nobody).</summary>
    public static void ForPerson(RouteGroupBuilder group, string name, Func<IReadOnlyList<object>, string?, string, object> project) =>
        group.MapGet($"/checks/{{checkId}}/views/{name}", async (string checkId, HttpRequest http, IEventStore store) =>
        {
            var (err, history) = await Load(checkId, store);
            if (err is not null) return err;
            var person = http.Headers[Streams.PersonIdHeader].ToString();
            return Results.Ok(project(history, Streams.IsValidId(person) ? person : null, Streams.Today()));
        });

    /// <summary>Like <see cref="Public"/>, but only for the holder of the owner key (403 otherwise).</summary>
    public static void Owner(RouteGroupBuilder group, string name, Func<IReadOnlyList<object>, string, object> project) =>
        group.MapGet($"/checks/{{checkId}}/views/{name}", async (string checkId, HttpRequest http, IEventStore store) =>
        {
            var (err, history) = await Load(checkId, store);
            if (err is not null) return err;
            if (!Streams.OwnerKeyMatches(http.Headers[Streams.OwnerKeyHeader].ToString(), CheckFacts.From(history).OwnerKeyHash))
                return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);
            return Results.Ok(project(history, Streams.Today()));
        });
}
