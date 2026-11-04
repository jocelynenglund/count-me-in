using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public record ShowInterestRequest(string? PersonId, string? Name, string? Email);

public static class ShowInterest
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/interest", Handle);

    private static async Task<IResult> Handle(string checkId, ShowInterestRequest req, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var name = (req.Name ?? "").Trim();
        var email = (req.Email ?? "").Trim();
        var personId = req.PersonId;

        var d = await CheckStore.Run(store, checkId, state =>
        {
            if (!state.Started) return Decision.Refuse(404, "No such interest check");
            if (state.Closed) return Decision.Refuse(409, "This interest check has closed");
            if (name.Length == 0) return Decision.Refuse(400, "What's your name?");
            if (name.Length > Streams.MaxNameLength) return Decision.Refuse(400, $"Your name must be {Streams.MaxNameLength} characters or fewer");
            if (email.Length > Streams.MaxEmailLength) return Decision.Refuse(400, $"Your email must be {Streams.MaxEmailLength} characters or fewer");
            if (!Streams.IsValidId(personId)) return Decision.Refuse(400, "invalid personId");
            if (state.IsIn(personId!)) return Decision.Refuse(409, "You're already counted in");
            return Decision.Accept(new { },
                ("InterestShown", new()
                {
                    ["checkId"] = checkId,
                    ["personId"] = personId,
                    ["person"] = Streams.PersonHandle(checkId, personId!),
                    ["name"] = name,
                    ["email"] = email.Length == 0 ? null : email,
                    ["on"] = Streams.Today(),
                }));
        });
        return d.ToResult();
    }
}
