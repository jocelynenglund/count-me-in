using ItsyBIT.Decisions;

namespace CountMeIn.Features.Checks;

/// <summary>Goal is null when the caller sent something that isn't a whole number.</summary>
public sealed record StartInterestCheck(string? CheckId, string? Title, string? Pitch, int? Goal, string? ClosesOn, string OwnerKey, string Today);

public static class StartInterestCheckSlice
{
    public static ItsyBIT.Decisions.Decision Decide(IReadOnlyList<object> history, StartInterestCheck cmd) =>
        throw new NotBuiltYetException();
}
