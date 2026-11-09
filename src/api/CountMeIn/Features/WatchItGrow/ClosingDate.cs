using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record ClosingDateView([property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, bool CanChange, string Earliest);

/// <summary>Closing Date: the date now, and whether it can still move (not once the owner has closed it). Owner only.</summary>
public static class ClosingDate
{
    public static ClosingDateView Project(IReadOnlyList<object> history, string today)
    {
        var c = CheckFacts.From(history);
        return new(c.ClosesOn, !c.ClosedByOwner, today);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "closing-date", (h, today) => Project(h, today));
}
