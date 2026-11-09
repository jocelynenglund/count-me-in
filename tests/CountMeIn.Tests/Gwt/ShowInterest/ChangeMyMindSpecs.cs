using CountMeIn.Domain;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Store;

namespace CountMeIn.Tests.Specs;

/// <summary>Change My Mind (the server's rules). The browser's twin is tests/gwt/show-interest/change-my-mind.test.mjs.</summary>
public class ChangeMyMindSpecs
{
    static readonly HistoryChapter Chapter = new("Show interest");

    const string Today = "2026-11-04";

    static readonly InterestCheckStarted Started = new("c1", "Board games", "", 10, null, Ids.Sha256Hex("k1"));
    static readonly InterestShown AnaIn = new("c1", "p1", Ids.PersonHandle("c1", "p1"), "Ana", null, Today);

    static ChangeMyMind Withdraw(string? personId = "p1", string checkId = "c1") => new(checkId, personId, Today);

    [Fact]
    public void Changing_your_mind() =>
        Chapter.Scenario("Changing your mind")
            .Given(Started, AnaIn)
            .When("Change My Mind", Withdraw(), ChangeMyMindSlice.Decide)
            .Then(new InterestWithdrawn("c1", "p1", Ids.PersonHandle("c1", "p1"), Today));

    [Fact]
    public void You_cant_take_back_what_you_never_gave() =>
        Chapter.Scenario("You can't take back what you never gave")
            .Given(Started)
            .When("Change My Mind", Withdraw(), ChangeMyMindSlice.Decide)
            .ThenNothing("You're not counted in", nameof(ErrorKind.Conflict));

    [Fact]
    public void Withdrawing_twice_is_refused() =>
        Chapter.Scenario("Withdrawing twice is refused")
            .Given(Started, AnaIn, new InterestWithdrawn("c1", "p1", Ids.PersonHandle("c1", "p1"), Today))
            .When("Change My Mind", Withdraw(), ChangeMyMindSlice.Decide)
            .ThenNothing("You're not counted in", nameof(ErrorKind.Conflict));

    [Fact]
    public void A_bad_person_id_is_not_counted_in() =>
        Chapter.Scenario("A person id with odd characters is not counted in")
            .Given(Started, AnaIn)
            .When("Change My Mind", Withdraw(personId: "a b"), ChangeMyMindSlice.Decide)
            .ThenNothing("You're not counted in", nameof(ErrorKind.Conflict));

    [Fact]
    public void No_such_interest_check() =>
        Chapter.Scenario("No such interest check")
            .Given()
            .When("Change My Mind", Withdraw(checkId: "c9"), ChangeMyMindSlice.Decide)
            .ThenNothing("No such interest check", nameof(ErrorKind.NotFound));
}
