using Microsoft.AspNetCore.Http;

namespace CountMeIn.Store;

/// <summary>Which budget a write draws from. Count in and withdraw share one, and so do the owner's commands.</summary>
public enum WriteGroup { StartCheck, CountInOrWithdraw, OwnerCommand, MyChecksView }

/// <summary>
/// Per-IP fixed-window limits for the open write endpoints (the API is public and unauthenticated).
/// In memory, thread-safe, no background work: expired windows are swept lazily when the table grows.
/// The host resolves the real client IP behind App Service, so <c>RemoteIpAddress</c> is the caller.
/// </summary>
public sealed class WriteLimits
{
    public const string TooMany = "Too many tries. Wait a little and try again.";
    private const int SweepAbove = 5000;

    public readonly record struct Limit(int Max, TimeSpan Window);

    public static IReadOnlyDictionary<WriteGroup, Limit> Defaults { get; } = new Dictionary<WriteGroup, Limit>
    {
        [WriteGroup.StartCheck] = new(10, TimeSpan.FromHours(1)),
        [WriteGroup.CountInOrWithdraw] = new(60, TimeSpan.FromMinutes(10)),
        [WriteGroup.OwnerCommand] = new(60, TimeSpan.FromMinutes(10)),
        [WriteGroup.MyChecksView] = new(30, TimeSpan.FromMinutes(1)),
    };

    private sealed class Bucket { public DateTimeOffset Start; public int Count; }

    private readonly IReadOnlyDictionary<WriteGroup, Limit> _limits;
    private readonly TimeProvider _clock;
    private readonly Dictionary<(WriteGroup, string), Bucket> _buckets = new();
    private readonly object _lock = new();

    public WriteLimits() : this(Defaults, TimeProvider.System) { }
    public WriteLimits(IReadOnlyDictionary<WriteGroup, Limit> limits, TimeProvider clock) { _limits = limits; _clock = clock; }

    /// <summary>Spends one try. False when the caller is over the limit; retryAfterSeconds says when the window ends.</summary>
    public bool TryAcquire(WriteGroup group, string key, out int retryAfterSeconds)
    {
        var limit = _limits[group];
        var now = _clock.GetUtcNow();
        lock (_lock)
        {
            if (_buckets.Count >= SweepAbove) Sweep(now);
            if (!_buckets.TryGetValue((group, key), out var b) || now - b.Start >= limit.Window)
            {
                _buckets[(group, key)] = new Bucket { Start = now, Count = 1 };
                retryAfterSeconds = 0;
                return true;
            }
            if (b.Count < limit.Max) { b.Count++; retryAfterSeconds = 0; return true; }
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((b.Start + limit.Window - now).TotalSeconds));
            return false;
        }
    }

    private void Sweep(DateTimeOffset now)
    {
        foreach (var k in _buckets.Where(kv => now - kv.Value.Start >= _limits[kv.Key.Item1].Window).Select(kv => kv.Key).ToList())
            _buckets.Remove(k);
        // Still huge after dropping the expired (a flood from many addresses): start over rather than grow.
        if (_buckets.Count >= SweepAbove * 2) _buckets.Clear();
    }

    /// <summary>Null when the caller may go on; otherwise the 429 to return.</summary>
    public IResult? Check(HttpContext ctx, WriteGroup group)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (TryAcquire(group, ip, out var retry)) return null;
        ctx.Response.Headers.RetryAfter = retry.ToString();
        return Results.Json(new { error = TooMany }, statusCode: StatusCodes.Status429TooManyRequests);
    }
}
