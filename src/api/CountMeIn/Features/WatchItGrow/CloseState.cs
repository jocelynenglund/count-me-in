using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record CloseStateView(string Status);

/// <summary>Close State: open, closed by the owner or closed by its date. Owner only.</summary>
public static class CloseState
{
    public static CloseStateView Project(IReadOnlyList<object> history, string today)
    {
        var c = CheckFacts.From(history);
        return new(c.ClosedByOwner ? "closed-by-owner" : c.ClosedByDate(today) ? "closed-by-date" : "open");
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "close-state", (h, today) => Project(h, today));
}
