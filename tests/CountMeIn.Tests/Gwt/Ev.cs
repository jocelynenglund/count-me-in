using CountMeIn.Domain;

namespace CountMeIn.Tests.Specs;

/// <summary>Plain events for the view specs. Today in every view scenario is 2026-11-09.</summary>
static class Ev
{
    public const string Today = "2026-11-09";

    public static InterestCheckStarted Started(string id = "c1", string title = "Board games", int goal = 10, string? closesOn = null, string key = "k1", string pitch = "Fridays") =>
        new(id, title, pitch, goal, closesOn, Ids.Sha256Hex(key));

    public static InterestShown In(string id, string personId, string name, string? email = null, string on = Today) =>
        new(id, personId, Ids.PersonHandle(id, personId), name, email, on);

    public static InterestWithdrawn Out(string id, string personId, string on = Today) =>
        new(id, personId, Ids.PersonHandle(id, personId), on);

    public static InterestCheckClosed Closed(string id = "c1") => new(id, Today);
    public static ThemeChosen Theme(string id, string theme) => new(id, theme);
    public static ClosingDateChanged Moved(string id, string? closesOn) => new(id, closesOn);
}
