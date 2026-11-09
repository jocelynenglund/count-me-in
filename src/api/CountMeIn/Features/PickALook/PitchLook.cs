using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchLookView(string CheckId, string Theme);

/// <summary>Pitch Look: which look the pitch page wears.</summary>
public static class PitchLook
{
    public static PitchLookView Project(IReadOnlyList<object> history)
    {
        var c = CheckFacts.From(history);
        return new(CheckFacts.CheckIdOf(history[0]), c.Theme);
    }

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Public(g, "pitch-look", (h, _) => Project(h));
}
