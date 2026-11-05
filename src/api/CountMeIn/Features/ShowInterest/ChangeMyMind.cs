using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record ChangeMyMind(string CheckId, string? PersonId, string Today);

public static class ChangeMyMindSlice
{
    public static Decision Decide(IReadOnlyList<object> history, ChangeMyMind cmd)
    {
        var check = CheckState.From(history);
        if (!check.Started) return Decision.NotFound("No such interest check");
        if (!Streams.IsValidId(cmd.PersonId) || !check.IsIn(cmd.PersonId!)) return Decision.Conflict("You're not counted in");

        return Decision.Accept(new InterestWithdrawn(cmd.CheckId, cmd.PersonId!, Streams.PersonHandle(cmd.CheckId, cmd.PersonId!), cmd.Today));
    }
}

public record WithdrawInterestRequest(string? PersonId);

public static class ChangeMyMindEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/withdraw", Handle);

    private static async Task<IResult> Handle(string checkId, WithdrawInterestRequest req, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var cmd = new ChangeMyMind(checkId, req.PersonId, Streams.Today());
        var d = await CheckStore.Execute(store, checkId, history => ChangeMyMindSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
