using CountMeIn.Domain;
using CountMeIn.Store;
using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.ShowInterest;

public sealed record CountMeInCommand(string CheckId, string? PersonId, string? Name, string? Email, string Today);

public static class CountMeInSlice
{
    public static Decision Decide(IReadOnlyList<object> history, CountMeInCommand cmd)
    {
        if (!history.OfType<InterestCheckStarted>().Any()) return Decision.NotFound("No such interest check");
        if (history.OfType<InterestCheckClosed>().Any()) return Decision.Conflict("This interest check has closed");

        var name = (cmd.Name ?? "").Trim();
        var email = (cmd.Email ?? "").Trim();
        if (name.Length == 0) return Decision.Invalid("What's your name?");
        if (name.Length > Ids.MaxNameLength) return Decision.Invalid($"Your name must be {Ids.MaxNameLength} characters or fewer");
        if (email.Length > Ids.MaxEmailLength) return Decision.Invalid($"Your email must be {Ids.MaxEmailLength} characters or fewer");
        if (!Ids.IsValidId(cmd.PersonId)) return Decision.Invalid("invalid personId");
        if (IsIn(history, cmd.PersonId!)) return Decision.Conflict("You're already counted in");

        return Decision.Accept(new InterestShown(
            cmd.CheckId, cmd.PersonId!, Ids.PersonHandle(cmd.CheckId, cmd.PersonId!), name,
            email.Length == 0 ? null : email, cmd.Today));
    }

    /// <summary>The fold this rule needs: is the person in now (their latest show or withdraw decides)?</summary>
    private static bool IsIn(IReadOnlyList<object> history, string personId)
    {
        var isIn = false;
        foreach (var e in history)
        {
            if (e is InterestShown s && s.PersonId == personId) isIn = true;
            else if (e is InterestWithdrawn w && w.PersonId == personId) isIn = false;
        }
        return isIn;
    }
}

public record ShowInterestRequest(string? PersonId, string? Name, string? Email);

public static class CountMeInEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/interest", Handle);

    private static async Task<IResult> Handle(string checkId, ShowInterestRequest req, HttpContext ctx, WriteLimits limits, ILockingEventStore store)
    {
        if (limits.Check(ctx, WriteGroup.CountInOrWithdraw) is { } limited) return limited;
        if (!Ids.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var cmd = new CountMeInCommand(checkId, req.PersonId, req.Name, req.Email, Ids.Today());
        var d = await CheckStore.Execute(store, checkId, history => CountMeInSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
