using CountMeIn.Domain;
using CountMeIn.Store;
using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.PickALook;

/// <summary>S7 Choose Theme: the owner picks how the pitch looks. OwnerKey is what the caller sent (raw); the stream holds its hash.</summary>
public sealed record ChooseTheme(string CheckId, string? OwnerKey, string? Theme);

public static class ChooseThemeSlice
{
    public static Decision Decide(IReadOnlyList<object> history, ChooseTheme cmd)
    {
        // What this rule reads of the history: whether it started, and the owner key's hash.
        var started = history.OfType<InterestCheckStarted>().FirstOrDefault();
        if (started is null) return Decision.NotFound("There is no such interest check.");
        if (!Ids.OwnerKeyMatches(cmd.OwnerKey, started.OwnerKeyHash)) return Decision.Forbidden("Only the person who started it can change its look.");
        if (cmd.Theme is null || !Themes.All.Contains(cmd.Theme)) return Decision.Invalid("Pick one of the themes.");

        return Decision.Accept(new ThemeChosen(cmd.CheckId, cmd.Theme));
    }
}

public record ChooseThemeRequest(string? Theme);

public static class ChooseThemeEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/theme", Handle);

    private static async Task<IResult> Handle(string checkId, ChooseThemeRequest req, HttpRequest http, WriteLimits limits, ILockingEventStore store)
    {
        var ctx = http.HttpContext;
        if (limits.Check(ctx, WriteGroup.OwnerCommand) is { } limited) return limited;
        if (!Ids.IsValidId(checkId)) return Results.NotFound(new { error = "There is no such interest check." });
        var cmd = new ChooseTheme(checkId, http.Headers[Ids.OwnerKeyHeader].ToString(), req.Theme);
        var d = await CheckStore.Execute(store, checkId, history => ChooseThemeSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
