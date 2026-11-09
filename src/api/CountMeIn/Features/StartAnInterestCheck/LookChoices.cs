using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.StartAnInterestCheck;

public sealed record LookChoice(string Theme, string Label, bool Picked);
public sealed record LookChoicesView(IReadOnlyList<LookChoice> Choices);

/// <summary>Look Choices: the looks on offer, the check's own picked (classic when there is no check or no choice yet).</summary>
public static class LookChoices
{
    public static LookChoicesView Project(IReadOnlyList<object> history)
    {
        var theme = history.OfType<ThemeChosen>().LastOrDefault()?.Theme ?? "classic";
        return new(Themes.Labels.Select(t => new LookChoice(t.Theme, t.Label, t.Theme == theme)).ToList());
    }

    public static void Map(RouteGroupBuilder g)
    {
        g.MapGet("/views/look-choices", () => Results.Ok(Project([])));
        g.MapGet("/checks/{checkId}/views/look-choices", Handle);
    }

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        return Results.Ok(Project(history));
    }
}
