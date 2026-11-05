using System.Globalization;
using System.Text.Json;
using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

/// <summary>Goal is null when the caller sent something that isn't a whole number. OwnerKey is the fresh raw key; only its hash is stored.</summary>
public sealed record StartInterestCheck(string? CheckId, string? Title, string? Pitch, int? Goal, string? ClosesOn, string OwnerKey, string Today);

public static class StartInterestCheckSlice
{
    public static Decision Decide(IReadOnlyList<object> history, StartInterestCheck cmd)
    {
        if (!Streams.IsValidId(cmd.CheckId)) return Decision.Invalid("invalid checkId");
        var title = (cmd.Title ?? "").Trim();
        if (title.Length == 0) return Decision.Invalid("What's the idea called?");
        if (title.Length > Streams.MaxTitleLength) return Decision.Invalid($"The title must be {Streams.MaxTitleLength} characters or fewer");
        var pitch = (cmd.Pitch ?? "").Trim();
        if (pitch.Length > Streams.MaxPitchLength) return Decision.Invalid($"The pitch must be {Streams.MaxPitchLength} characters or fewer");

        if (cmd.Goal is not { } goal || goal < 1 || goal > Streams.MaxGoal)
            return Decision.Invalid($"How many people would make it worth doing? Between 1 and {Streams.MaxGoal}.");

        string? closesOn = string.IsNullOrWhiteSpace(cmd.ClosesOn) ? null : cmd.ClosesOn.Trim();
        if (closesOn is not null)
        {
            if (!DateOnly.TryParseExact(closesOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return Decision.Invalid("The closing date must look like 2026-12-31");
            if (string.CompareOrdinal(closesOn, cmd.Today) < 0)
                return Decision.Invalid("The closing date has already passed");
        }

        if (history.OfType<InterestCheckStarted>().Any()) return Decision.Conflict("This interest check already exists");

        return Decision.Accept(new InterestCheckStarted(cmd.CheckId!, title, pitch, goal, closesOn, Streams.Sha256Hex(cmd.OwnerKey)));
    }
}

public record StartCheckRequest(string? CheckId, string? Title, string? Pitch, JsonElement? Goal, string? ClosesOn);
public record StartCheckResponse(string CheckId, string OwnerKey);

public static class StartInterestCheckEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks", Handle);

    private static async Task<IResult> Handle(StartCheckRequest req, ILockingEventStore store)
    {
        // Ids reach the filesystem, so a bad one never gets as far as the store.
        if (!Streams.IsValidId(req.CheckId)) return Results.BadRequest(new { error = "invalid checkId" });

        int? goal = req.Goal is { ValueKind: JsonValueKind.Number } g && g.TryGetInt32(out var n) ? n : null;
        var ownerKey = Streams.NewOwnerKey();
        var cmd = new StartInterestCheck(req.CheckId, req.Title, req.Pitch, goal, req.ClosesOn, ownerKey, Streams.Today());

        var d = await CheckStore.Execute(store, req.CheckId!, history => StartInterestCheckSlice.Decide(history, cmd));
        return d.ToResult(new StartCheckResponse(req.CheckId!, ownerKey));
    }
}
