using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using EventStore.Core;

namespace CountMeIn.Features.Checks;

public record StartCheckRequest(string? CheckId, string? Title, string? Pitch, JsonElement? Goal, string? ClosesOn);
public record StartCheckResponse(string CheckId, string OwnerKey);

public static class StartCheck
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks", Handle);

    private static async Task<IResult> Handle(StartCheckRequest req, ILockingEventStore store)
    {
        if (!Streams.IsValidId(req.CheckId)) return Bad("invalid checkId");
        var title = (req.Title ?? "").Trim();
        if (title.Length == 0) return Bad("What's the idea called?");
        if (title.Length > Streams.MaxTitleLength) return Bad($"The title must be {Streams.MaxTitleLength} characters or fewer");
        var pitch = (req.Pitch ?? "").Trim();
        if (pitch.Length > Streams.MaxPitchLength) return Bad($"The pitch must be {Streams.MaxPitchLength} characters or fewer");

        if (req.Goal is not { ValueKind: JsonValueKind.Number } g || !g.TryGetInt32(out var goal) || goal < 1 || goal > Streams.MaxGoal)
            return Bad($"How many people would make it worth doing? Between 1 and {Streams.MaxGoal}.");

        string? closesOn = string.IsNullOrWhiteSpace(req.ClosesOn) ? null : req.ClosesOn.Trim();
        if (closesOn is not null)
        {
            if (!DateOnly.TryParseExact(closesOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return Bad("The closing date must look like 2026-12-31");
            if (string.CompareOrdinal(closesOn, Streams.Today()) < 0)
                return Bad("The closing date has already passed");
        }

        var checkId = req.CheckId!;
        var ownerKey = Streams.NewOwnerKey();
        var d = await CheckStore.Run(store, checkId, state =>
            state.Started
                ? Decision.Refuse(409, "This interest check already exists")
                : Decision.Accept(new StartCheckResponse(checkId, ownerKey),
                    ("InterestCheckStarted", new()
                    {
                        ["checkId"] = checkId,
                        ["title"] = title,
                        ["pitch"] = pitch,
                        ["goal"] = goal,
                        ["closesOn"] = closesOn,
                        ["ownerKeyHash"] = Streams.Sha256Hex(ownerKey),
                    })));
        return d.ToResult();
    }

    private static IResult Bad(string error) => Results.BadRequest(new { error });
}
