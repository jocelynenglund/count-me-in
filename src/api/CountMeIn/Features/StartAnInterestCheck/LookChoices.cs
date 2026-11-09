using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record LookChoice(string Theme, string Label, bool Picked);
public sealed record LookChoicesView(IReadOnlyList<LookChoice> Choices);

/// <summary>Look Choices: the looks on offer, the check's own picked (classic when there is no check or no choice yet).</summary>
public static class LookChoices
{
    public static LookChoicesView Project(IReadOnlyList<object> history)
    {
        var theme = CheckFacts.From(history).Theme;
        return new(ChooseThemeSlice.ThemeLabels.Select(t => new LookChoice(t.Theme, t.Label, t.Theme == theme)).ToList());
    }

    public static void Map(RouteGroupBuilder g)
    {
        g.MapGet("/views/look-choices", () => Results.Ok(Project([])));
        ViewRoutes.Public(g, "look-choices", (h, _) => Project(h));
    }
}
