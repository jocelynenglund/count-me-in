using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public record WithdrawInterestRequest(string? PersonId);

public static class WithdrawInterest
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/withdraw", Handle);

    private static async Task<IResult> Handle(string checkId, WithdrawInterestRequest req, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var personId = req.PersonId;

        var d = await CheckStore.Run(store, checkId, state =>
        {
            if (!state.Started) return Decision.Refuse(404, "No such interest check");
            if (!Streams.IsValidId(personId) || !state.IsIn(personId!)) return Decision.Refuse(409, "You're not counted in");
            return Decision.Accept(new { },
                ("InterestWithdrawn", new()
                {
                    ["checkId"] = checkId,
                    ["personId"] = personId,
                    ["person"] = Streams.PersonHandle(checkId, personId!),
                    ["on"] = Streams.Today(),
                }));
        });
        return d.ToResult();
    }
}
