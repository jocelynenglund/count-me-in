using CountMeIn.Domain;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class PitchCountSpecs
{
    static readonly HistoryChapter Chapter = new("Pitch page, one view per part");

    [Fact]
    public void The_count_of_whos_in() =>
        Chapter.Scenario("The count of who's in")
            .Given(Started(goal: 3), In("c1", "p1", "Ana"), In("c1", "p2", "Ben"), Out("c1", "p1"))
            .ThenView("Pitch Count", h => PitchCount.Project(h, Today), new PitchCountView(1, 3, true, 3, 1));

    [Fact]
    public void Closed_they_were_in() =>
        Chapter.Scenario("Closed: they were in")
            .Given(Started(goal: 3, closesOn: "2026-11-08"), In("c1", "p1", "Ana"))
            .ThenView("Pitch Count", h => PitchCount.Project(h, Today), new PitchCountView(1, 3, false, 3, 1));

    [Fact]
    public void The_meter_at_most_10_boxes_filled_in_proportion() =>
        Chapter.Scenario("The meter: at most 10 boxes, filled in proportion")
            .Given(Started(goal: 24), In("c1", "p1", "Ana"), In("c1", "p2", "Ben"), In("c1", "p3", "Dev"))
            .ThenView("Pitch Count", h => PitchCount.Project(h, Today), new PitchCountView(3, 24, true, 10, 1));
}
