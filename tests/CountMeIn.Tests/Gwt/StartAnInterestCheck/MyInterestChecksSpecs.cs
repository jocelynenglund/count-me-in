using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class MyInterestChecksSpecs
{
    static readonly HistoryChapter Chapter = new("Start page, one view per part");

    static readonly object[] TwoChecks =
    [
        Started("c1", "Board games", 10, key: "k1"), In("c1", "p1", "Ana"),
        Started("c2", "Advent of Vibes", 24, key: "k2"),
    ];

    [Fact]
    public void Your_checks_newest_first() =>
        Chapter.Scenario("Your checks, newest first")
            .Given(TwoChecks)
            .ThenView("My Interest Checks", h => MyInterestChecks.Project(h, [new("c1", "k1"), new("c2", "k2")], Today),
                new MyInterestChecksView([new("c2", "Advent of Vibes", 0, 24, true), new("c1", "Board games", 1, 10, true)]));

    [Fact]
    public void None_started_here_yet() =>
        Chapter.Scenario("None started here yet")
            .Given()
            .ThenView("My Interest Checks", h => MyInterestChecks.Project(h, [], Today), new MyInterestChecksView([]));

    [Fact]
    public void A_passed_closing_date_shows_as_closed() =>
        Chapter.Scenario("A passed closing date shows as closed")
            .Given(Started("c1", "Board games", 10, "2026-11-08", "k1"))
            .ThenView("My Interest Checks", h => MyInterestChecks.Project(h, [new("c1", "k1")], Today),
                new MyInterestChecksView([new("c1", "Board games", 0, 10, false)]));

    [Fact]
    public void A_link_with_the_wrong_key_is_left_out() =>
        Chapter.Scenario("A link with the wrong key is left out")
            .Given(TwoChecks.Take(2).ToArray())
            .ThenView("My Interest Checks", h => MyInterestChecks.Project(h, [new("c1", "wrong")], Today), new MyInterestChecksView([]));
}
