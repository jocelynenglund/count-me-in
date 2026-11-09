using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.StartAnInterestCheck;

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
        foreach (var group in history.GroupBy(CheckIdOf))
        {
            var started = group.OfType<InterestCheckStarted>().FirstOrDefault();
            if (started is null) continue;
            if (!links.Any(l => l.CheckId == group.Key && Ids.OwnerKeyMatches(l.OwnerKey, started.OwnerKeyHash))) continue;

            // This view's fold of one check: how many are in, the closing date now, and whether the owner closed it.
            var isIn = new HashSet<string>();
            var closesOn = started.ClosesOn; var byOwner = false;
            foreach (var e in group)
                switch (e)
                {
                    case InterestShown s: isIn.Add(s.PersonId); break;
                    case InterestWithdrawn w: isIn.Remove(w.PersonId); break;
                    case InterestCheckClosed: byOwner = true; break;
                    case ClosingDateChanged c: closesOn = c.ClosesOn; break;
                }
            var closed = byOwner || (closesOn is not null && string.CompareOrdinal(closesOn, today) < 0);
            mine.Add(new MyCheck(group.Key, started.Title, isIn.Count, started.Goal, !closed));
        }
        mine.Reverse();
        return new(mine);
    }

    /// <summary>Which check an event belongs to: the history here is several checks' events side by side.</summary>
    private static string CheckIdOf(object e) => e switch
    {
        InterestCheckStarted x => x.CheckId,
        InterestShown x => x.CheckId,
        InterestWithdrawn x => x.CheckId,
        InterestCheckClosed x => x.CheckId,
        ThemeChosen x => x.CheckId,
        ClosingDateChanged x => x.CheckId,
        _ => "",
    };

    public static void Map(RouteGroupBuilder g) => g.MapPost("/views/my-interest-checks", async (MyInterestChecksRequest req, HttpContext ctx, WriteLimits limits, IEventStore store) =>
    {
        if (limits.Check(ctx, WriteGroup.MyChecksView) is { } limited) return limited;
        var links = req.Links ?? [];
        if (links.Count > MaxLinks) return Results.BadRequest(new { error = $"at most {MaxLinks} links" });
        var streams = new List<(DateTimeOffset First, IReadOnlyList<object> History)>();
        foreach (var id in links.Select(l => l.CheckId).Where(Ids.IsValidId).Distinct())
        {
            var events = await CheckStore.Read(store, id);
            if (events.Count == 0) continue;
            streams.Add((events[0].Timestamp, CheckStore.Unwrap(events)));
        }
        var history = streams.OrderBy(s => s.First).SelectMany(s => s.History).ToList();
        return Results.Ok(Project(history, links, Ids.Today()));
    });
}
