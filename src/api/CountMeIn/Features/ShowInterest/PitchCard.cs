using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchCardView(string Title, string Pitch, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, bool ClosedByDate);

/// <summary>Pitch Card: the pitch itself and whether it is open, reached or closed. No names, no emails.</summary>
public static class PitchCard
{
    public static PitchCardView Project(IReadOnlyList<object> history, string today)
    {
        var c = CheckFacts.From(history);
        return new(c.Title, c.Pitch, c.Status(today), c.ClosesOn, c.ClosedByDate(today));
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Public(g, "pitch-card", (h, today) => Project(h, today));
}
