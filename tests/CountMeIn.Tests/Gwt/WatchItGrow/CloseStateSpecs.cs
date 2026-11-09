using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class CloseStateSpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void Open_close_it_is_offered() =>
        Chapter.Scenario("Open: Close it is offered")
            .Given(Started())
            .ThenView("Close State", h => CloseState.Project(h, Today), new CloseStateView("open"));

    [Fact]
    public void Closed_by_the_owner() =>
        Chapter.Scenario("Closed by the owner")
            .Given(Started(), Closed())
            .ThenView("Close State", h => CloseState.Project(h, Today), new CloseStateView("closed-by-owner"));

    [Fact]
    public void Closed_by_its_date() =>
        Chapter.Scenario("Closed by its date")
            .Given(Started(closesOn: "2026-11-08"))
            .ThenView("Close State", h => CloseState.Project(h, Today), new CloseStateView("closed-by-date"));
}
