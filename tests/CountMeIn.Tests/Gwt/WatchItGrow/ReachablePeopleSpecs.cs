using CountMeIn.Domain;
using CountMeIn.Features.WatchItGrow;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class ReachablePeopleSpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void Those_in_who_want_to_hear() =>
        Chapter.Scenario("Those in who want to hear")
            .Given(Started(), In("c1", "p1", "Ana", "ana@example.com"), In("c1", "p2", "Ben"))
            .ThenView("Reachable People", h => ReachablePeople.Project(h), new ReachablePeopleView([new("Ana", "ana@example.com")]));

    [Fact]
    public void Someone_who_took_themselves_off_isnt_mailed() =>
        Chapter.Scenario("Someone who took themselves off isn't mailed")
            .Given(Started(), In("c1", "p1", "Ana", "ana@example.com"), In("c1", "p3", "Dev", "dev@example.com"), Out("c1", "p3"))
            .ThenView("Reachable People", h => ReachablePeople.Project(h), new ReachablePeopleView([new("Ana", "ana@example.com")]));
}
