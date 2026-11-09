using CountMeIn.Domain;
using CountMeIn.Features.WatchItGrow;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class ClosingDateSpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void The_date_can_move_from_today_on() =>
        Chapter.Scenario("The date can move, from today on")
            .Given(Started(closesOn: "2026-11-30"))
            .ThenView("Closing Date", h => ClosingDate.Project(h, Today), new ClosingDateView("2026-11-30", true, "2026-11-09"));

    [Fact]
    public void The_newest_date_is_shown() =>
        Chapter.Scenario("The newest date is shown")
            .Given(Started(closesOn: "2026-11-30"), Moved("c1", null))
            .ThenView("Closing Date", h => ClosingDate.Project(h, Today), new ClosingDateView(null, true, "2026-11-09"));

    [Fact]
    public void A_check_you_closed_cant_change_its_date() =>
        Chapter.Scenario("A check you closed can't change its date")
            .Given(Started(closesOn: "2026-11-30"), Closed())
            .ThenView("Closing Date", h => ClosingDate.Project(h, Today), new ClosingDateView("2026-11-30", false, "2026-11-09"));
}
