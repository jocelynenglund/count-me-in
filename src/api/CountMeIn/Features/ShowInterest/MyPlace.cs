using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record MyPlaceView(string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

/// <summary>My Place: where this visitor stands: can join, in (by the name they gave) or closed. Closed wins.</summary>
public static class MyPlace
{
    public static MyPlaceView Project(IReadOnlyList<object> history, string? personId, string today)
    {
        var c = CheckFacts.From(history);
        if (c.Closed(today)) return new("closed", null);
        var me = personId is null ? null : c.People.FirstOrDefault(p => p.PersonId == personId);
        return me is null ? new("can-join", null) : new("in", me.Name);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.ForPerson(g, "my-place", (h, person, today) => Project(h, person, today));
}
