using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record SignupDay(string Day, int In);
public sealed record SignupsPerDayView(IReadOnlyList<SignupDay> Days);

/// <summary>Sign-ups Per Day: the last 7 days, empty days included; no chart (empty) before anyone has joined. Owner only.</summary>
public static class SignupsPerDay
{
    public static SignupsPerDayView Project(IReadOnlyList<object> history, string today)
    {
        var shown = CheckFacts.From(history).AllShown;
        if (shown.Count == 0) return new([]);
        var end = DateTime.ParseExact(today, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var days = Enumerable.Range(0, 7).Select(i => end.AddDays(i - 6).ToString("yyyy-MM-dd"))
            .Select(d => new SignupDay(d, shown.Count(s => s.On == d))).ToList();
        return new(days);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "signups-per-day", (h, today) => Project(h, today));
}
