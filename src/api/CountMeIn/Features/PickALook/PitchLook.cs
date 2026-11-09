using CountMeIn.Domain;
using CountMeIn.Store;
using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.PickALook;

public sealed record PitchLookView(string CheckId, string Theme);

/// <summary>Pitch Look: which look the pitch page wears.</summary>
public static class PitchLook
{
    public static PitchLookView Project(IReadOnlyList<object> history)
    {
        var checkId = history.OfType<InterestCheckStarted>().First().CheckId;
        var theme = history.OfType<ThemeChosen>().LastOrDefault()?.Theme ?? "classic";
        return new(checkId, theme);
    }

    public static void Map(RouteGroupBuilder g) => g.MapGet("/checks/{checkId}/views/pitch-look", Handle);

    private static async Task<IResult> Handle(string checkId, IEventStore store)
    {
        if (!Ids.IsValidId(checkId)) return Results.BadRequest(new { error = "invalid checkId" });
        var events = await CheckStore.Read(store, checkId);
        if (events.Count == 0) return Results.NotFound(new { error = "No such interest check" });
        var history = CheckStore.Unwrap(events);
        return Results.Ok(Project(history));
    }
}
