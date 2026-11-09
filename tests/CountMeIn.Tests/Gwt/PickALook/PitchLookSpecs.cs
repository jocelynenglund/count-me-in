using CountMeIn.Domain;
using CountMeIn.Features.PickALook;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class PitchLookSpecs
{
    static readonly HistoryChapter Chapter = new("Pitch page, one view per part");

    [Fact]
    public void Classic_until_a_theme_is_chosen() =>
        Chapter.Scenario("Classic until a theme is chosen")
            .Given(Started())
            .ThenView("Pitch Look", h => PitchLook.Project(h), new PitchLookView("c1", "classic"));

    [Fact]
    public void Christmas_once_chosen() =>
        Chapter.Scenario("Christmas once chosen")
            .Given(Started(), Theme("c1", "christmas"))
            .ThenView("Pitch Look", h => PitchLook.Project(h), new PitchLookView("c1", "christmas"));

    [Fact]
    public void The_newest_choice_wins() =>
        Chapter.Scenario("The newest choice wins")
            .Given(Started(), Theme("c1", "christmas"), Theme("c1", "classic"))
            .ThenView("Pitch Look", h => PitchLook.Project(h), new PitchLookView("c1", "classic"));
}
