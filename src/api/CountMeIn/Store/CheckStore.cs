using System.Text.Json;
using EventStore.Core;
using EventStore.Core.Shared;
using CountMeIn.Domain;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Http;

namespace CountMeIn.Store;

public static class CheckStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Dictionary<string, Type> EventTypes = new[]
    {
        typeof(InterestCheckStarted), typeof(InterestShown), typeof(InterestWithdrawn), typeof(InterestCheckClosed), typeof(ThemeChosen), typeof(ClosingDateChanged),
    }.ToDictionary(t => t.Name);

    /// <summary>The stored events as the plain records the slices decide over. Unknown types are skipped.</summary>
    public static IReadOnlyList<object> Unwrap(IEnumerable<Event> stored) =>
        stored.Where(e => EventTypes.ContainsKey(e.Type))
              .Select(e => e.Payload.Deserialize(EventTypes[e.Type], Json)!)
              .ToList();

    /// <summary>
    /// The store adapter for a command slice: under the check stream's write lock, load the events,
    /// unwrap them, call <paramref name="decide"/> and append what it decided (null fields are left out,
    /// as before). The lock is the store's optimistic-concurrency equivalent: two simultaneous requests
    /// can't both pass the same check.
    /// </summary>
    public static Task<Decision> Execute(ILockingEventStore store, string checkId, Func<IReadOnlyList<object>, Decision> decide)
    {
        var stream = Ids.Check(checkId);
        return store.ExecuteInStreamLockAsync(CountMeInModule.Module, stream, async s =>
        {
            var d = decide(Unwrap(await s.ReadAsync()));
            foreach (var e in d.Events)
            {
                await s.AppendAsync(new Event
                {
                    Id = IdGenerator.NewEventId(),
                    Type = e.GetType().Name,
                    StreamId = stream,
                    Timestamp = DateTimeOffset.UtcNow,
                    Payload = JsonSerializer.SerializeToElement(e, e.GetType(), Json),
                });
            }
            return d;
        });
    }

    /// <summary>The refusal as the JSON error the web app reads, or 200 with the body.</summary>
    public static IResult ToResult(this Decision d, object? body = null) =>
        d.Accepted ? Results.Ok(body ?? new { }) : Results.Json(new { error = d.Error }, statusCode: d.Kind.StatusCode());

    public static Task<IReadOnlyList<Event>> Read(IEventStore store, string checkId) =>
        store.ReadStreamAsync(CountMeInModule.Module, Ids.Check(checkId));

    /// <summary>A plain event as a flat object: its type, then its fields as stored (camelCase, nulls left out).</summary>
    public static Dictionary<string, object?> Flat(object e)
    {
        var flat = new Dictionary<string, object?> { ["type"] = e.GetType().Name };
        foreach (var prop in JsonSerializer.SerializeToElement(e, e.GetType(), Json).EnumerateObject())
            flat[prop.Name] = prop.Value.Clone();
        return flat;
    }

    /// <summary>Adds each stored event's time as "at" to a log view (one entry per known event, in stream order).</summary>
    public static IEnumerable<Dictionary<string, object?>> WithTimes(IEnumerable<Dictionary<string, object?>> log, IEnumerable<Event> stored) =>
        log.Zip(stored.Where(e => EventTypes.ContainsKey(e.Type)), (entry, e) => new Dictionary<string, object?>(entry) { ["at"] = e.Timestamp.ToString("o") });
}
