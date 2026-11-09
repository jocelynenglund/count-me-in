using CountMeIn.Domain;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Store;

namespace CountMeIn.Tests.Specs;

/// <summary>Count Me In (the server's rules). The browser's twin is tests/gwt/show-interest/count-me-in.test.mjs.</summary>
public class CountMeInSpecs
{
    static readonly HistoryChapter Chapter = new("Show interest");

    const string Today = "2026-11-04";

    static readonly InterestCheckStarted Started = new("c1", "Board games", "", 10, null, Ids.Sha256Hex("k1"));
    static readonly InterestShown AnaIn = new("c1", "p1", Ids.PersonHandle("c1", "p1"), "Ana", null, Today);
    static readonly InterestWithdrawn AnaOut = new("c1", "p1", Ids.PersonHandle("c1", "p1"), Today);
    static readonly InterestCheckClosed Closed = new("c1", Today);

    static CountMeInCommand CountIn(string? name = "Ana", string? personId = "p1", string? email = null, string checkId = "c1") =>
        new(checkId, personId, name, email, Today);

    [Fact]
    public void Counting_yourself_in() =>
        Chapter.Scenario("Counting yourself in")
            .Given(Started)
            .When("Count Me In", CountIn(), CountMeInSlice.Decide)
            .Then(new InterestShown("c1", "p1", Ids.PersonHandle("c1", "p1"), "Ana", null, Today));

    [Fact]
    public void An_email_is_kept_when_given() =>
        Chapter.Scenario("An email is kept when you give one")
            .Given(Started)
            .When("Count Me In", CountIn(email: " ana@example.com "), CountMeInSlice.Decide)
            .Then(Gwt.Event<InterestShown>(new { personId = "p1", email = "ana@example.com" }));

    [Fact]
    public void You_need_a_name() =>
        Chapter.Scenario("You need a name")
            .Given(Started)
            .When("Count Me In", CountIn(name: ""), CountMeInSlice.Decide)
            .ThenNothing("What's your name?", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_name_has_a_limit() =>
        Chapter.Scenario("A name can't be longer than 60 characters")
            .Given(Started)
            .When("Count Me In", CountIn(name: new string('n', 61)), CountMeInSlice.Decide)
            .ThenNothing("Your name must be 60 characters or fewer", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_email_has_a_limit() =>
        Chapter.Scenario("An email can't be longer than 200 characters")
            .Given(Started)
            .When("Count Me In", CountIn(email: new string('e', 201)), CountMeInSlice.Decide)
            .ThenNothing("Your email must be 200 characters or fewer", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_person_id_must_be_valid() =>
        Chapter.Scenario("A person id with odd characters is refused")
            .Given(Started)
            .When("Count Me In", CountIn(personId: "a b"), CountMeInSlice.Decide)
            .ThenNothing("invalid personId", nameof(ErrorKind.Invalid));

    [Fact]
    public void Counting_in_twice_is_refused() =>
        Chapter.Scenario("Counting in twice is refused")
            .Given(Started, AnaIn)
            .When("Count Me In", CountIn(), CountMeInSlice.Decide)
            .ThenNothing("You're already counted in", nameof(ErrorKind.Conflict));

    [Fact]
    public void Changed_your_mind_then_back_in() =>
        Chapter.Scenario("Changed your mind, then back in")
            .Given(Started, AnaIn, AnaOut)
            .When("Count Me In", CountIn(), CountMeInSlice.Decide)
            .Then(Gwt.Event<InterestShown>(new { personId = "p1" }));

    [Fact]
    public void A_closed_check_takes_no_more_interest() =>
        Chapter.Scenario("A closed check takes no more interest")
            .Given(Started, Closed)
            .When("Count Me In", CountIn(name: "Ben", personId: "p2"), CountMeInSlice.Decide)
            .ThenNothing("This interest check has closed", nameof(ErrorKind.Conflict));

    [Fact]
    public void No_such_interest_check() =>
        Chapter.Scenario("No such interest check")
            .Given()
            .When("Count Me In", CountIn(checkId: "c9"), CountMeInSlice.Decide)
            .ThenNothing("No such interest check", nameof(ErrorKind.NotFound));
}
