using ItsyBIT.Decisions;

namespace CountMeIn.Features.Checks;

public sealed record CountMeInCommand(string CheckId, string? PersonId, string? Name, string? Email, string Today);

public static class CountMeInSlice
{
    public static ItsyBIT.Decisions.Decision Decide(IReadOnlyList<object> history, CountMeInCommand cmd) =>
        throw new NotBuiltYetException();
}
