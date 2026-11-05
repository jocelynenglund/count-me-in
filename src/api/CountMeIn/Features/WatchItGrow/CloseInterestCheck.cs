using ItsyBIT.Decisions;

namespace CountMeIn.Features.Checks;

/// <summary>OwnerKey is what the caller sent (the raw key); the stream holds only its hash.</summary>
public sealed record CloseInterestCheck(string CheckId, string? OwnerKey, string Today);

public static class CloseInterestCheckSlice
{
    public static ItsyBIT.Decisions.Decision Decide(IReadOnlyList<object> history, CloseInterestCheck cmd) =>
        throw new NotBuiltYetException();
}
