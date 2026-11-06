using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>S7 Choose Theme: the owner picks how the pitch looks. OwnerKey is what the caller sent (raw); the stream holds its hash.</summary>
public sealed record ChooseTheme(string CheckId, string? OwnerKey, string? Theme);

public static class ChooseThemeSlice
{
    /// <summary>The looks there are. Keep in step with src/web/lib/themes.mjs.</summary>
    public static readonly IReadOnlySet<string> Themes = new HashSet<string> { "classic", "christmas" };

    public static Decision Decide(IReadOnlyList<object> history, ChooseTheme cmd)
    {
        var check = CheckState.From(history);
        if (!check.Started) return Decision.NotFound("There is no such interest check.");
        if (!Streams.OwnerKeyMatches(cmd.OwnerKey, check.OwnerKeyHash)) return Decision.Forbidden("Only the person who started it can change its look.");
        if (cmd.Theme is null || !Themes.Contains(cmd.Theme)) return Decision.Invalid("Pick one of the themes.");

        return Decision.Accept(new ThemeChosen(cmd.CheckId, cmd.Theme));
    }
}

public record ChooseThemeRequest(string? Theme);

public static class ChooseThemeEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/theme", Handle);

    private static async Task<IResult> Handle(string checkId, ChooseThemeRequest req, HttpRequest http, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "There is no such interest check." });
        var cmd = new ChooseTheme(checkId, http.Headers[Streams.OwnerKeyHeader].ToString(), req.Theme);
        var d = await CheckStore.Execute(store, checkId, history => ChooseThemeSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
