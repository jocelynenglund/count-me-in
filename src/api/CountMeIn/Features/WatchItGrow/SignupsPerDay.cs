using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.WatchItGrow;

public sealed record SignupDay(string Day, int In);
public sealed record SignupsPerDayView(IReadOnlyList<SignupDay> Days);

/// <summary>Sign-ups Per Day: the last 7 days, empty days included; no chart (empty) before anyone has joined. Owner only.</summary>
public static class SignupsPerDay
{
    public static SignupsPerDayView Project(IReadOnlyList<object> history, string today)
    {
        var shown = history.OfType<InterestShown>().ToList();  // every sign-up counts on its day, even if the person left since
        if (shown.Count == 0) return new([]);
        var end = DateTime.ParseExact(today, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var days = Enumerable.Range(0, 7).Select(i => end.AddDays(i - 6).ToString("yyyy-MM-dd"))
            .Select(d => new SignupDay(d, shown.Count(s => s.On == d))).ToList();
        return new(days);
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/signups-per-day", Handle);

    private static async Task<IResult> Handle(string checkId, HttpRequest http, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        var owner = history.OfType<InterestCheckStarted>().FirstOrDefault()?.OwnerKeyHash;
        if (!Ids.OwnerKeyMatches(http.Headers[Ids.OwnerKeyHeader].ToString(), owner))
            return Results.Json(new { error = "Only the owner can see this" }, statusCode: 403);
        return Results.Ok(Project(history, Ids.Today()));
    }
}
