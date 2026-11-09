using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchCountView(int Count, int Goal, bool Open, int MeterBoxes, int MeterOn);

/// <summary>Pitch Count: how many are in, against the goal, and the meter.</summary>
public static class PitchCount
{
    public static PitchCountView Project(IReadOnlyList<object> history, string today)
    {
        var c = CheckFacts.From(history);
        var (boxes, on) = c.Meter();
        return new(c.Count, c.Goal, !c.Closed(today), boxes, on);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Public(g, "pitch-count", (h, today) => Project(h, today));
}
