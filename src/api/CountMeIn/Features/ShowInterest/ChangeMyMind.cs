using CountMeIn.Domain;
using CountMeIn.Store;
using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.ShowInterest;

public sealed record ChangeMyMind(string CheckId, string? PersonId, string Today);

public static class ChangeMyMindSlice
{
    public static Decision Decide(IReadOnlyList<object> history, ChangeMyMind cmd)
    {
        if (!history.OfType<InterestCheckStarted>().Any()) return Decision.NotFound("No such interest check");
        if (!Ids.IsValidId(cmd.PersonId) || !IsIn(history, cmd.PersonId!)) return Decision.Conflict("You're not counted in");

        return Decision.Accept(new InterestWithdrawn(cmd.CheckId, cmd.PersonId!, Ids.PersonHandle(cmd.CheckId, cmd.PersonId!), cmd.Today));
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

public record WithdrawInterestRequest(string? PersonId);

public static class ChangeMyMindEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/withdraw", Handle);

    private static async Task<IResult> Handle(string checkId, WithdrawInterestRequest req, HttpContext ctx, WriteLimits limits, ILockingEventStore store)
    {
        if (limits.Check(ctx, WriteGroup.CountInOrWithdraw) is { } limited) return limited;
        if (!Ids.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var cmd = new ChangeMyMind(checkId, req.PersonId, Ids.Today());
        var d = await CheckStore.Execute(store, checkId, history => ChangeMyMindSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
