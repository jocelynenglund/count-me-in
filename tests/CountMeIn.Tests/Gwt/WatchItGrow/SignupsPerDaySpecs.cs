using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class SignupsPerDaySpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void The_last_7_days_empty_days_included() =>
        Chapter.Scenario("The last 7 days, empty days included")
            .Given(Started(), In("c1", "p1", "Ana", on: "2026-11-05"), In("c1", "p2", "Ben", on: "2026-11-05"), In("c1", "p3", "Dev", on: "2026-11-09"))
            .ThenView("Sign-ups Per Day", h => SignupsPerDay.Project(h, Today), new SignupsPerDayView(
            [
                new("2026-11-03", 0), new("2026-11-04", 0), new("2026-11-05", 2), new("2026-11-06", 0),
                new("2026-11-07", 0), new("2026-11-08", 0), new("2026-11-09", 1),
            ]));

    [Fact]
    public void Nobody_yet_no_chart() =>
        Chapter.Scenario("Nobody yet: no chart")
            .Given(Started())
            .ThenView("Sign-ups Per Day", h => SignupsPerDay.Project(h, Today), new SignupsPerDayView([]));
}
