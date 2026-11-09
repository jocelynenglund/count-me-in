using CountMeIn.Domain;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Store;

namespace CountMeIn.Tests.Specs;

/// <summary>Public Log (the server's view): what anyone with the check's link gets from
/// GET /checks/{id}/events, and what the browser's Pitch view is worked out from. Only the
/// allowlisted fields, so no name, email, personId or key hash ever leaves.</summary>
public class PublicLogSpecs
{
    static readonly HistoryChapter Chapter = new("Show interest");

    const string Today = "2026-11-04";
    static readonly string Ana = Ids.PersonHandle("c1", "p1");

    static readonly InterestCheckStarted Started = new("c1", "Board games", "Fridays", 4, "2026-11-30", Ids.Sha256Hex("k1"));
    static readonly InterestShown AnaIn = new("c1", "p1", Ana, "Ana Secretname", "ana@example.com", Today);
    static readonly InterestWithdrawn AnaOut = new("c1", "p1", Ana, Today);
    static readonly InterestCheckClosed Closed = new("c1", Today);

    [Fact]
    public void The_public_log_of_a_new_check() =>
        Chapter.Scenario("The public log of a new check")
            .Given(Started)
            .ThenView("Public Log", PublicLog.Project, new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "InterestCheckStarted", ["checkId"] = "c1", ["title"] = "Board games",
                    ["pitch"] = "Fridays", ["goal"] = 4, ["closesOn"] = "2026-11-30",
                },
            });

    [Fact]
    public void People_show_as_an_anonymous_handle_only() =>
        Chapter.Scenario("People show as an anonymous handle, never a name or email")
            .Given(Started, AnaIn)
            .ThenView("Public Log", h => PublicLog.Project(h).Skip(1), new object[]
            {
                new Dictionary<string, object?> { ["type"] = "InterestShown", ["checkId"] = "c1", ["person"] = Ana, ["on"] = Today },
            });

    [Fact]
    public void Changing_your_mind_and_closing_show_in_order() =>
        Chapter.Scenario("Changing your mind and closing show up in order")
            .Given(Started, AnaIn, AnaOut, Closed)
            .ThenView("Public Log", h => PublicLog.Project(h).Select(e => e["type"]), new[]
            {
                "InterestCheckStarted", "InterestShown", "InterestWithdrawn", "InterestCheckClosed",
            });

    /// <summary>The fields each entry carries, in order, as one string: compared whole, so an extra field fails.</summary>
    static string Fields(IReadOnlyList<Dictionary<string, object?>> log) => string.Join(" | ", log.Select(e => string.Join(",", e.Keys)));

    [Fact]
    public void Nothing_private_leaves() =>
        Chapter.Scenario("Nothing private leaves: no name, email, personId or key hash")
            .Given(Started, AnaIn, AnaOut, Closed)
            .ThenView("Public Log", h => Fields(PublicLog.Project(h)),
                "type,checkId,title,pitch,goal,closesOn | type,checkId,person,on | type,checkId,person,on | type,checkId,on");

    [Fact]
    public void A_check_with_no_closing_date_leaves_it_out() =>
        Chapter.Scenario("A check with no closing date leaves it out")
            .Given(Started with { ClosesOn = null })
            .ThenView("Public Log", h => PublicLog.Project(h)[0].ContainsKey("closesOn"), false);
}
