using ItsyBIT.Decisions;

namespace CountMeIn.Features.Checks;

public sealed record ChangeMyMind(string CheckId, string? PersonId, string Today);

public static class ChangeMyMindSlice
{
    public static ItsyBIT.Decisions.Decision Decide(IReadOnlyList<object> history, ChangeMyMind cmd) =>
        throw new NotBuiltYetException();
}
