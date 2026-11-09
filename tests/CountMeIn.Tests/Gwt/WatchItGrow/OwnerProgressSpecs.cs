using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class OwnerProgressSpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void How_far_it_has_come() =>
        Chapter.Scenario("How far it has come")
            .Given(Started(goal: 10, closesOn: "2026-11-30"), In("c1", "p1", "Ana"), In("c1", "p2", "Ben"))
            .ThenView("Owner Progress", h => OwnerProgress.Project(h, Today), new OwnerProgressView("Board games", 2, 10, 8, "open", "2026-11-30", 10, 2));

    [Fact]
    public void Goal_reached() =>
        Chapter.Scenario("Goal reached")
            .Given(Started(goal: 1), In("c1", "p1", "Ana"))
            .ThenView("Owner Progress", h => OwnerProgress.Project(h, Today), new OwnerProgressView("Board games", 1, 1, 0, "reached", null, 1, 1));

    [Fact]
    public void Closed_by_its_date() =>
        Chapter.Scenario("Closed by its date")
            .Given(Started(goal: 10, closesOn: "2026-11-08"))
            .ThenView("Owner Progress", h => OwnerProgress.Project(h, Today), new OwnerProgressView("Board games", 0, 10, 10, "closed", "2026-11-08", 10, 0));
}
