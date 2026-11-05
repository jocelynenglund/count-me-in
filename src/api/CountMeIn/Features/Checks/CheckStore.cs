using System.Text.Json;
using EventStore.Core;
using EventStore.Core.Shared;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Http;

namespace CountMeIn.Features.Checks;

/// <summary>What the rules need to know about one check, folded from its stream.</summary>
public sealed class CheckState
{
    public bool Started { get; private set; }
    public bool Closed { get; private set; }
    public string? OwnerKeyHash { get; private set; }
    private readonly HashSet<string> _in = [];

    public bool IsIn(string personId) => _in.Contains(personId);

    /// <summary>Folded from the plain events the slices decide over.</summary>
    public static CheckState From(IReadOnlyList<object> history)
    {
        var s = new CheckState();
        foreach (var e in history)
            switch (e)
            {
                case InterestCheckStarted st: s.Started = true; s.OwnerKeyHash = st.OwnerKeyHash; break;
                case InterestShown sh: s._in.Add(sh.PersonId); break;
                case InterestWithdrawn w: s._in.Remove(w.PersonId); break;
                case InterestCheckClosed: s.Closed = true; break;
            }
        return s;
    }
}

public static class CheckStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Dictionary<string, Type> EventTypes = new[]
    {
        typeof(InterestCheckStarted), typeof(InterestShown), typeof(InterestWithdrawn), typeof(InterestCheckClosed),
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
        var stream = Streams.Check(checkId);
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
        store.ReadStreamAsync(CountMeInModule.Module, Streams.Check(checkId));

    private static readonly Dictionary<string, string[]> PublicFields = new()
    {
        ["InterestCheckStarted"] = ["checkId", "title", "pitch", "goal", "closesOn"],
        ["InterestShown"] = ["checkId", "person", "on"],
        ["InterestWithdrawn"] = ["checkId", "person", "on"],
        ["InterestCheckClosed"] = ["checkId", "on"],
    };

    /// <summary>A flat event for the public: only allowlisted fields, so a new private field can't leak by accident.</summary>
    public static Dictionary<string, object?> ToPublic(Event e)
    {
        var flat = new Dictionary<string, object?> { ["type"] = e.Type };
        if (PublicFields.TryGetValue(e.Type, out var fields))
            foreach (var f in fields)
                if (e.Payload.ValueKind == JsonValueKind.Object && e.Payload.TryGetProperty(f, out var v)) flat[f] = v.Clone();
        flat["at"] = e.Timestamp.ToString("o");
        return flat;
    }

    /// <summary>A flat event for the owner: everything except the key hash.</summary>
    public static Dictionary<string, object?> ToOwner(Event e)
    {
        var flat = new Dictionary<string, object?> { ["type"] = e.Type };
        if (e.Payload.ValueKind == JsonValueKind.Object)
            foreach (var prop in e.Payload.EnumerateObject())
                if (prop.Name is not ("ownerKeyHash" or "type" or "at")) flat[prop.Name] = prop.Value.Clone();
        flat["at"] = e.Timestamp.ToString("o");
        return flat;
    }
}
