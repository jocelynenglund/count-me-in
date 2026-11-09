using CountMeIn.Domain;
using CountMeIn.Features.WatchItGrow;
using CountMeIn.Store;

namespace CountMeIn.Tests.Specs;

/// <summary>Owner Log (the server's view): what the holder of the owner key gets from
/// GET /checks/{id}/owner-events, and what the browser's How It's Going view is worked out from.
/// Everything but the key hash; anyone without the key gets nothing.</summary>
public class OwnerLogSpecs
{
    static readonly HistoryChapter Chapter = new("Watch it grow");

    const string Today = "2026-11-04";
    static readonly string Ana = Ids.PersonHandle("c1", "p1");

    static readonly InterestCheckStarted Started = new("c1", "Board games", "Fridays", 4, null, Ids.Sha256Hex("k1"));
    static readonly InterestShown AnaIn = new("c1", "p1", Ana, "Ana", "ana@example.com", Today);
    static readonly InterestShown BoIn = new("c1", "p2", Ids.PersonHandle("c1", "p2"), "Bo", null, Today);

    [Fact]
    public void The_owner_sees_who_counted_themselves_in() =>
        Chapter.Scenario("The owner sees who counted themselves in")
            .Given(Started, AnaIn)
            .ThenView("Owner Log", h => OwnerLog.Project(h, "k1")!.Skip(1), new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "InterestShown", ["checkId"] = "c1", ["personId"] = "p1", ["person"] = Ana,
                    ["name"] = "Ana", ["email"] = "ana@example.com", ["on"] = Today,
                },
            });

    [Fact]
    public void The_key_hash_never_leaves() =>
        Chapter.Scenario("The owner key's hash never leaves")
            .Given(Started)
            .ThenView("Owner Log", h => OwnerLog.Project(h, "k1")!, new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "InterestCheckStarted", ["checkId"] = "c1", ["title"] = "Board games",
                    ["pitch"] = "Fridays", ["goal"] = 4,
                },
            });

    [Fact]
    public void Only_the_key_hash_is_held_back() =>
        Chapter.Scenario("Everything but the key hash, field by field")
            .Given(Started, AnaIn)
            .ThenView("Owner Log", h => string.Join(" | ", OwnerLog.Project(h, "k1")!.Select(e => string.Join(",", e.Keys))),
                "type,checkId,title,pitch,goal | type,checkId,personId,person,name,email,on");

    [Fact]
    public void No_email_given_leaves_it_out() =>
        Chapter.Scenario("Someone who gave no email has none in the log")
            .Given(Started, BoIn)
            .ThenView("Owner Log", h => OwnerLog.Project(h, "k1")![1].ContainsKey("email"), false);

    [Fact]
    public void The_wrong_key_sees_nothing() =>
        Chapter.Scenario("The wrong key sees nothing")
            .Given(Started, AnaIn)
            .ThenView("Owner Log", h => OwnerLog.Project(h, "wrong"), null);

    [Fact]
    public void No_key_sees_nothing() =>
        Chapter.Scenario("No key sees nothing")
            .Given(Started, AnaIn)
            .ThenView("Owner Log", h => OwnerLog.Project(h, ""), null);
}
