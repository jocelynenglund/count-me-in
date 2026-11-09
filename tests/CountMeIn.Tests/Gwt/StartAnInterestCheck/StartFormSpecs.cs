using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class StartFormSpecs
{
    static readonly HistoryChapter Chapter = new("Start page, one view per part");

    [Fact]
    public void An_empty_form_starts_from_today_and_a_goal_of_10() =>
        Chapter.Scenario("An empty form starts from today and a goal of 10")
            .Given()
            .ThenView("Start Form", h => StartForm.Project(h, Today), new StartFormView("2026-11-09", 10));
}
