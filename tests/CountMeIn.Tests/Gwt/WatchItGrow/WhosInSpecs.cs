using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class WhosInSpecs
{
    static readonly HistoryChapter Chapter = new("Owner page, one view per part");

    [Fact]
    public void Whos_in_now_and_who_left_an_email() =>
        Chapter.Scenario("Who's in now, and who left an email")
            .Given(Started(), In("c1", "p1", "Ana", "ana@example.com"), In("c1", "p2", "Ben"), In("c1", "p3", "Dev", "dev@example.com"), Out("c1", "p3"))
            .ThenView("Who's In", h => WhosIn.Project(h), new WhosInView([new("Ana", true), new("Ben", false)]));

    [Fact]
    public void Nobody_yet() =>
        Chapter.Scenario("Nobody yet")
            .Given(Started())
            .ThenView("Who's In", h => WhosIn.Project(h), new WhosInView([]));
}
