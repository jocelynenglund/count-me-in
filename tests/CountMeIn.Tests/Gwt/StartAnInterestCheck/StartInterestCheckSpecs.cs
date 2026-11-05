using CountMeIn.Features.Checks;

namespace CountMeIn.Tests.Specs;

/// <summary>Start Interest Check (the server's rules). The browser's twin is tests/gwt/start-an-interest-check;
/// the scenarios and refusal texts match, plus the limits only the server enforces.</summary>
public class StartInterestCheckSpecs
{
    static readonly HistoryChapter Chapter = new("Start an interest check");

    const string Key = "k1";
    const string Today = "2026-11-04";

    static StartInterestCheck Start(string? title = "Saturday board-game club", int? goal = 10, string? closesOn = null,
        string? pitch = "Monthly games at the library", string? checkId = "c1") =>
        new(checkId, title, pitch, goal, closesOn, Key, Today);

    static readonly InterestCheckStarted Started =
        new("c1", "Board games", "", 10, null, Streams.Sha256Hex(Key));

    [Fact]
    public void Starting_an_interest_check() =>
        Chapter.Scenario("Starting an interest check")
            .Given()
            .When("Start Interest Check", Start(), StartInterestCheckSlice.Decide)
            .Then(new InterestCheckStarted("c1", "Saturday board-game club", "Monthly games at the library", 10, null, Streams.Sha256Hex(Key)));

    [Fact]
    public void A_closing_date_is_kept() =>
        Chapter.Scenario("A closing date today or later is kept")
            .Given()
            .When("Start Interest Check", Start(closesOn: "2026-12-31"), StartInterestCheckSlice.Decide)
            .Then(Gwt.Event<InterestCheckStarted>(new { checkId = "c1", closesOn = "2026-12-31" }));

    [Fact]
    public void The_title_and_pitch_are_trimmed() =>
        Chapter.Scenario("The title and pitch are trimmed")
            .Given()
            .When("Start Interest Check", Start(title: "  Board games  ", pitch: " fun "), StartInterestCheckSlice.Decide)
            .Then(Gwt.Event<InterestCheckStarted>(new { title = "Board games", pitch = "fun" }));

    [Fact]
    public void An_interest_check_needs_a_title() =>
        Chapter.Scenario("An interest check needs a title")
            .Given()
            .When("Start Interest Check", Start(title: " "), StartInterestCheckSlice.Decide)
            .ThenNothing("What's the idea called?", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_title_has_a_limit() =>
        Chapter.Scenario("The title can't be longer than 120 characters")
            .Given()
            .When("Start Interest Check", Start(title: new string('t', 121)), StartInterestCheckSlice.Decide)
            .ThenNothing("The title must be 120 characters or fewer", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_pitch_has_a_limit() =>
        Chapter.Scenario("The pitch can't be longer than 1000 characters")
            .Given()
            .When("Start Interest Check", Start(pitch: new string('p', 1001)), StartInterestCheckSlice.Decide)
            .ThenNothing("The pitch must be 1000 characters or fewer", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_goal_is_at_least_1() =>
        Chapter.Scenario("The goal is at least 1")
            .Given()
            .When("Start Interest Check", Start(goal: 0), StartInterestCheckSlice.Decide)
            .ThenNothing("How many people would make it worth doing? Between 1 and 100000.", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_goal_has_a_ceiling() =>
        Chapter.Scenario("The goal can't be above 100000")
            .Given()
            .When("Start Interest Check", Start(goal: 100001), StartInterestCheckSlice.Decide)
            .ThenNothing("How many people would make it worth doing? Between 1 and 100000.", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_goal_must_be_a_number() =>
        Chapter.Scenario("A goal that isn't a whole number is refused")
            .Given()
            .When("Start Interest Check", Start(goal: null), StartInterestCheckSlice.Decide)
            .ThenNothing("How many people would make it worth doing? Between 1 and 100000.", nameof(ErrorKind.Invalid));

    [Fact]
    public void A_closing_date_in_the_past_is_refused() =>
        Chapter.Scenario("A closing date in the past is refused")
            .Given()
            .When("Start Interest Check", Start(closesOn: "2026-11-01"), StartInterestCheckSlice.Decide)
            .ThenNothing("The closing date has already passed", nameof(ErrorKind.Invalid));

    [Fact]
    public void A_closing_date_must_be_a_date() =>
        Chapter.Scenario("A closing date must look like 2026-12-31")
            .Given()
            .When("Start Interest Check", Start(closesOn: "next friday"), StartInterestCheckSlice.Decide)
            .ThenNothing("The closing date must look like 2026-12-31", nameof(ErrorKind.Invalid));

    [Fact]
    public void The_check_id_must_be_valid() =>
        Chapter.Scenario("A check id with odd characters is refused")
            .Given()
            .When("Start Interest Check", Start(checkId: "../etc"), StartInterestCheckSlice.Decide)
            .ThenNothing("invalid checkId", nameof(ErrorKind.Invalid));

    [Fact]
    public void Starting_twice_is_refused() =>
        Chapter.Scenario("Starting the same interest check twice is refused")
            .Given(Started)
            .When("Start Interest Check", Start(), StartInterestCheckSlice.Decide)
            .ThenNothing("This interest check already exists", nameof(ErrorKind.Conflict));
}
