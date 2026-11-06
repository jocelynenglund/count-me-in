using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>The Public Log view: flat events with only allowlisted fields, so no name, email,
/// personId or key hash leaves, and a new private field can't leak by accident.
/// The browser's Pitch view is worked out from this.</summary>
public static class PublicLog
{
    private static readonly Dictionary<string, string[]> Fields = new()
    {
        [nameof(InterestCheckStarted)] = ["checkId", "title", "pitch", "goal", "closesOn"],
        [nameof(InterestShown)] = ["checkId", "person", "on"],
        [nameof(InterestWithdrawn)] = ["checkId", "person", "on"],
        [nameof(InterestCheckClosed)] = ["checkId", "on"],
        [nameof(ThemeChosen)] = ["checkId", "theme"],
    };

    public static IReadOnlyList<Dictionary<string, object?>> Project(IReadOnlyList<object> history) =>
        history.Select(e =>
        {
            var all = CheckStore.Flat(e);
            var kept = new Dictionary<string, object?> { ["type"] = all["type"] };
            foreach (var f in Fields.GetValueOrDefault(e.GetType().Name, []))
                if (all.TryGetValue(f, out var v)) kept[f] = v;
            return kept;
        }).ToList();
}

/// <summary>The store adapter for the Public Log.</summary>
public static class GetEvents
{
    public static void Map(RouteGroupBuilder group) => group.MapGet("/checks/{checkId}/events", Handle);

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        return Results.Ok(CheckStore.WithTimes(PublicLog.Project(CheckStore.Unwrap(events)), events));
    }
}
