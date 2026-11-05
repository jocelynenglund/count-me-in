using EventStore.Core;
using ItsyBIT.Decisions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record CountMeInCommand(string CheckId, string? PersonId, string? Name, string? Email, string Today);

public static class CountMeInSlice
{
    public static Decision Decide(IReadOnlyList<object> history, CountMeInCommand cmd)
    {
        var check = CheckState.From(history);
        if (!check.Started) return Decision.NotFound("No such interest check");
        if (check.Closed) return Decision.Conflict("This interest check has closed");

        var name = (cmd.Name ?? "").Trim();
        var email = (cmd.Email ?? "").Trim();
        if (name.Length == 0) return Decision.Invalid("What's your name?");
        if (name.Length > Streams.MaxNameLength) return Decision.Invalid($"Your name must be {Streams.MaxNameLength} characters or fewer");
        if (email.Length > Streams.MaxEmailLength) return Decision.Invalid($"Your email must be {Streams.MaxEmailLength} characters or fewer");
        if (!Streams.IsValidId(cmd.PersonId)) return Decision.Invalid("invalid personId");
        if (check.IsIn(cmd.PersonId!)) return Decision.Conflict("You're already counted in");

        return Decision.Accept(new InterestShown(
            cmd.CheckId, cmd.PersonId!, Streams.PersonHandle(cmd.CheckId, cmd.PersonId!), name,
            email.Length == 0 ? null : email, cmd.Today));
    }
}

public record ShowInterestRequest(string? PersonId, string? Name, string? Email);

public static class CountMeInEndpoint
{
    public static void Map(RouteGroupBuilder group) => group.MapPost("/checks/{checkId}/interest", Handle);

    private static async Task<IResult> Handle(string checkId, ShowInterestRequest req, ILockingEventStore store)
    {
        if (!Streams.IsValidId(checkId)) return Results.NotFound(new { error = "No such interest check" });
        var cmd = new CountMeInCommand(checkId, req.PersonId, req.Name, req.Email, Streams.Today());
        var d = await CheckStore.Execute(store, checkId, history => CountMeInSlice.Decide(history, cmd));
        return d.ToResult();
    }
}
