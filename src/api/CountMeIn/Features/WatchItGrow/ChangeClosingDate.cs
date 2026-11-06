using System.Globalization;
using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>S8 Change Closing Date: the owner moves the deadline, or removes it (no date). OwnerKey is the raw key the caller sent.</summary>
public sealed record ChangeClosingDate(string CheckId, string? OwnerKey, string? ClosesOn, string Today);

public static class ChangeClosingDateSlice
{
    public static Decision Decide(IReadOnlyList<object> history, ChangeClosingDate cmd)
    {
        var check = CheckState.From(history);
        if (!check.Started) return Decision.NotFound("There is no such interest check.");
        if (!Streams.OwnerKeyMatches(cmd.OwnerKey, check.OwnerKeyHash)) return Decision.Forbidden("Only the person who started it can change its date.");
        if (check.Closed) return Decision.Conflict("You closed this one.");

        string? closesOn = string.IsNullOrWhiteSpace(cmd.ClosesOn) ? null : cmd.ClosesOn.Trim();
        if (closesOn is not null)
        {
            if (!DateOnly.TryParseExact(closesOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return Decision.Invalid("The closing date must look like 2026-12-31");
            if (string.CompareOrdinal(closesOn, cmd.Today) < 0) return Decision.Invalid("Pick a date from today on.");
        }
        return Decision.Accept(new ClosingDateChanged(cmd.CheckId, closesOn));
    }
}

public record ChangeClosingDateRequest(string? ClosesOn);

public static class ChangeClosingDateEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/closing-date", Handle);

    private static async Task<IResult> Handle(string checkId, ChangeClosingDateRequest req, HttpRequest http, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "There is no such interest check." });
        var cmd = new ChangeClosingDate(checkId, http.Headers[Streams.OwnerKeyHeader].ToString(), req.ClosesOn, Streams.Today());
        var d = await CheckStore.Execute(store, checkId, history => ChangeClosingDateSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
