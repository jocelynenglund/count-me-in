using CountMeIn.Domain;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Store;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class PitchCardSpecs
{
    static readonly HistoryChapter Chapter = new("Pitch page, one view per part");

    [Fact]
    public void An_open_pitch() =>
        Chapter.Scenario("An open pitch")
            .Given(Started(closesOn: "2026-11-30"))
            .ThenView("Pitch Card", h => PitchCard.Project(h, Today), new PitchCardView("Board games", "Fridays", "open", "2026-11-30", false));

    [Fact]
    public void Goal_reached_its_on() =>
        Chapter.Scenario("Goal reached: it's on")
            .Given(Started(goal: 1), In("c1", "p1", "Ana"))
            .ThenView("Pitch Card", h => PitchCard.Project(h, Today), new PitchCardView("Board games", "Fridays", "reached", null, false));

    [Fact]
    public void The_owner_closed_it() =>
        Chapter.Scenario("The owner closed it")
            .Given(Started(), Closed())
            .ThenView("Pitch Card", h => PitchCard.Project(h, Today), new PitchCardView("Board games", "Fridays", "closed", null, false));

    [Fact]
    public void The_closing_date_has_passed() =>
        Chapter.Scenario("The closing date has passed")
            .Given(Started(closesOn: "2026-11-08"))
            .ThenView("Pitch Card", h => PitchCard.Project(h, Today), new PitchCardView("Board games", "Fridays", "closed", "2026-11-08", true));

    [Fact]
    public void The_newest_closing_date_counts() =>
        Chapter.Scenario("The newest closing date counts")
            .Given(Started(closesOn: "2026-11-08"), Moved("c1", "2026-12-05"))
            .ThenView("Pitch Card", h => PitchCard.Project(h, Today), new PitchCardView("Board games", "Fridays", "open", "2026-12-05", false));
}
