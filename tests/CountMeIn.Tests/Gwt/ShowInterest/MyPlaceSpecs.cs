using CountMeIn.Features.Checks;
using static CountMeIn.Tests.Specs.Ev;

namespace CountMeIn.Tests.Specs;

public class MyPlaceSpecs
{
    static readonly HistoryChapter Chapter = new("Pitch page, one view per part");

    [Fact]
    public void Not_in_yet_you_can_count_yourself_in() =>
        Chapter.Scenario("Not in yet: you can count yourself in")
            .Given(Started())
            .ThenView("My Place", h => MyPlace.Project(h, "p1", Today), new MyPlaceView("can-join", null));

    [Fact]
    public void Youre_in_by_the_name_you_gave() =>
        Chapter.Scenario("You're in, by the name you gave")
            .Given(Started(), In("c1", "p1", "Ana"))
            .ThenView("My Place", h => MyPlace.Project(h, "p1", Today), new MyPlaceView("in", "Ana"));

    [Fact]
    public void Someone_else_being_in_doesnt_make_you_in() =>
        Chapter.Scenario("Someone else being in doesn't make you in")
            .Given(Started(), In("c1", "p2", "Ben"))
            .ThenView("My Place", h => MyPlace.Project(h, "p1", Today), new MyPlaceView("can-join", null));

    [Fact]
    public void You_took_yourself_off() =>
        Chapter.Scenario("You took yourself off")
            .Given(Started(), In("c1", "p1", "Ana"), Out("c1", "p1"))
            .ThenView("My Place", h => MyPlace.Project(h, "p1", Today), new MyPlaceView("can-join", null));

    [Fact]
    public void Closed_nothing_to_do() =>
        Chapter.Scenario("Closed: nothing to do")
            .Given(Started(), In("c1", "p1", "Ana"), Closed())
            .ThenView("My Place", h => MyPlace.Project(h, "p1", Today), new MyPlaceView("closed", null));
}
