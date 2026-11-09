using CountMeIn.Domain;
using CountMeIn.Features.StartAnInterestCheck;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class LookChoicesSpecs
{
    static readonly HistoryChapter Chapter = new("Start page, one view per part");

    [Fact]
    public void Before_a_check_exists_classic_is_picked() =>
        Chapter.Scenario("Before a check exists, classic is picked")
            .Given()
            .ThenView("Look Choices", h => LookChoices.Project(h),
                new LookChoicesView([new("classic", "Classic", true), new("christmas", "Christmas", false)]));

    [Fact]
    public void A_checks_own_look_is_picked() =>
        Chapter.Scenario("A check's own look is picked")
            .Given(Started(), Theme("c1", "christmas"))
            .ThenView("Look Choices", h => LookChoices.Project(h),
                new LookChoicesView([new("classic", "Classic", false), new("christmas", "Christmas", true)]));
}
