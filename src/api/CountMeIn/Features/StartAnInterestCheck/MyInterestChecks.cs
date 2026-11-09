using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record MyCheckLink(string CheckId, string OwnerKey);
public sealed record MyCheck(string CheckId, string Title, int Count, int Goal, bool Open);
public sealed record MyInterestChecksView(IReadOnlyList<MyCheck> Checks);
public sealed record MyInterestChecksRequest(List<MyCheckLink>? Links);

/// <summary>My Interest Checks: the checks this browser holds an owner key for, newest started first.
/// The history is several checks' events, each check's contiguous, in the order they were started.</summary>
public static class MyInterestChecks
{
    public const int MaxLinks = 50;

    public static MyInterestChecksView Project(IReadOnlyList<object> history, IReadOnlyList<MyCheckLink> links, string today)
    {
        var mine = new List<MyCheck>();
        foreach (var group in history.GroupBy(CheckFacts.CheckIdOf))
        {
            var facts = CheckFacts.From(group.ToList());
            if (!facts.Started) continue;
            if (!links.Any(l => l.CheckId == group.Key && Streams.OwnerKeyMatches(l.OwnerKey, facts.OwnerKeyHash))) continue;
            mine.Add(new MyCheck(group.Key, facts.Title, facts.Count, facts.Goal, !facts.Closed(today)));
        }
        mine.Reverse();
        return new(mine);
    }

    public static void Map(RouteGroupBuilder g) => g.MapPost("/views/my-interest-checks", async (MyInterestChecksRequest req, IEventStore store) =>
    {
        var links = req.Links ?? [];
        if (links.Count > MaxLinks) return Results.BadRequest(new { error = $"at most {MaxLinks} links" });
        var streams = new List<(DateTimeOffset First, IReadOnlyList<object> History)>();
        foreach (var id in links.Select(l => l.CheckId).Where(Streams.IsValidId).Distinct())
        {
            var events = await CheckStore.Read(store, id);
            if (events.Count == 0) continue;
            streams.Add((events[0].Timestamp, CheckStore.Unwrap(events)));
        }
        var history = streams.OrderBy(s => s.First).SelectMany(s => s.History).ToList();
        return Results.Ok(Project(history, links, Streams.Today()));
    });
}
