using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.WatchItGrow;

public sealed record WhoIsIn(string Name, bool LeftEmail);
public sealed record WhosInView(IReadOnlyList<WhoIsIn> People);

/// <summary>Who's In: who is in now, and whether they left an email (not the email). Owner only.</summary>
public static class WhosIn
{
    public static WhosInView Project(IReadOnlyList<object> history) =>
        new(PeopleIn(history).Select(p => new WhoIsIn(p.Name, !string.IsNullOrEmpty(p.Email))).ToList());

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

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/whos-in", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        var owner = history.OfType<InterestCheckStarted>().FirstOrDefault()?.OwnerKeyHash;
        if (!Ids.OwnerKeyMatches(http.Headers[Ids.OwnerKeyHeader].ToString(), owner))
            return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);
        return Results.Ok(Project(history));
    }
}
