using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record OwnerProgressView(string Title, int Count, int Goal, int ToGo, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, int MeterBoxes, int MeterOn);

/// <summary>Owner Progress: how far the check has come. Owner only.</summary>
public static class OwnerProgress
{
    public static OwnerProgressView Project(IReadOnlyList<object> history, string today)
    {
        var c = CheckFacts.From(history);
        var (boxes, on) = c.Meter();
        return new(c.Title, c.Count, c.Goal, Math.Max(0, c.Goal - c.Count), c.Status(today), c.ClosesOn, boxes, on);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "owner-progress", (h, today) => Project(h, today));
}
