using CountMeIn.Domain;
using CountMeIn.Features.WatchItGrow;
using CountMeIn.Store;

namespace CountMeIn.Tests.Specs;

/// <summary>Close Interest Check (the server's rules). The browser's twin is tests/gwt/watch-it-grow/close-interest-check.test.mjs.
/// Here the owner key is compared by its SHA-256 hash, which is all the stream keeps.</summary>
public class CloseInterestCheckSpecs
{
    static readonly HistoryChapter Chapter = new("Watch it grow");

    const string Today = "2026-11-04";

    static readonly InterestCheckStarted Started = new("c1", "Board games", "", 10, null, Ids.Sha256Hex("k1"));

    static CloseInterestCheck Close(string? ownerKey = "k1", string checkId = "c1") => new(checkId, ownerKey, Today);

    [Fact]
    public void Closing_an_interest_check() =>
        Chapter.Scenario("Closing an interest check")
            .Given(Started)
            .When("Close Interest Check", Close(), CloseInterestCheckSlice.Decide)
            .Then(new InterestCheckClosed("c1", Today));

    [Fact]
    public void Only_the_owner_can_close_it() =>
        Chapter.Scenario("Only the owner can close it")
            .Given(Started)
            .When("Close Interest Check", Close("wrong"), CloseInterestCheckSlice.Decide)
            .ThenNothing("Only the owner can close this", nameof(ErrorKind.Forbidden));

    [Fact]
    public void No_key_is_not_the_owner() =>
        Chapter.Scenario("No owner key is not the owner")
            .Given(Started)
            .When("Close Interest Check", Close(""), CloseInterestCheckSlice.Decide)
            .ThenNothing("Only the owner can close this", nameof(ErrorKind.Forbidden));

    [Fact]
    public void Closing_twice_is_refused() =>
        Chapter.Scenario("Closing twice is refused")
            .Given(Started, new InterestCheckClosed("c1", Today))
            .When("Close Interest Check", Close(), CloseInterestCheckSlice.Decide)
            .ThenNothing("It's already closed", nameof(ErrorKind.Conflict));

    [Fact]
    public void No_such_interest_check() =>
        Chapter.Scenario("No such interest check")
            .Given()
            .When("Close Interest Check", Close(checkId: "c9"), CloseInterestCheckSlice.Decide)
            .ThenNothing("No such interest check", nameof(ErrorKind.NotFound));
}
