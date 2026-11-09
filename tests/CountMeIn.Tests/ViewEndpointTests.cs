using System.Net;
using System.Text.Json;
using Shouldly;

namespace CountMeIn.Tests;

/// <summary>The view endpoints over HTTP: shapes, who may ask, and what never leaves.</summary>
public class ViewEndpointTests
{
    static readonly string[] OwnerViews = ["owner-progress", "signups-per-day", "whos-in", "reachable-people", "closing-date", "close-state"];
    static readonly string[] PublicViews = ["pitch-card", "pitch-count", "my-place", "pitch-look", "look-choices"];

    static async Task<(TestApi Api, string Key)> Started()
    {
        var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Join("demo", "p1", "Alma Secretname", "alma@example.com");
        return (api, key);
    }

    [Fact]
    public async Task Public_views_answer_200_and_leak_nothing_personal()
    {
        var (api, key) = await Started();
        await using var _ = api;
        foreach (var v in PublicViews)
        {
            var res = await api.Get($"checks/demo/views/{v}");
            res.StatusCode.ShouldBe(HttpStatusCode.OK, v);
            var text = await res.Content.ReadAsStringAsync();
            text.ShouldNotContain("Alma", Case.Insensitive, v);
            text.ShouldNotContain("alma@", Case.Insensitive, v);
            text.ShouldNotContain("p1", Case.Sensitive, v);
            text.ShouldNotContain(key, Case.Sensitive, v);
            text.ShouldNotContain("ownerKeyHash", Case.Sensitive, v);
        }
    }

    [Fact]
    public async Task Pitch_views_have_the_right_shape()
    {
        var (api, _) = await Started();
        await using var _a = api;
        var card = await TestApi.Json(await api.Get("checks/demo/views/pitch-card"));
        card.GetProperty("title").GetString().ShouldBe("A board game night");
        card.GetProperty("status").GetString().ShouldBe("open");
        card.TryGetProperty("closesOn", out _).ShouldBeFalse();
        var count = await TestApi.Json(await api.Get("checks/demo/views/pitch-count"));
        count.GetProperty("count").GetInt32().ShouldBe(1);
        count.GetProperty("goal").GetInt32().ShouldBe(4);
        count.GetProperty("meterBoxes").GetInt32().ShouldBe(4);
        count.GetProperty("meterOn").GetInt32().ShouldBe(1);
        (await TestApi.Json(await api.Get("checks/demo/views/pitch-look"))).GetProperty("theme").GetString().ShouldBe("classic");
        var looks = await TestApi.Json(await api.Get("checks/demo/views/look-choices"));
        looks.GetProperty("choices").GetArrayLength().ShouldBe(2);
        var form = await TestApi.Json(await api.Get("views/start-form"));
        form.GetProperty("defaultGoal").GetInt32().ShouldBe(10);
        var firstLooks = await TestApi.Json(await api.Get("views/look-choices"));
        firstLooks.GetProperty("choices")[0].GetProperty("picked").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task My_place_is_in_for_your_own_person_id_and_can_join_for_anyone_else()
    {
        var (api, _) = await Started();
        await using var _a = api;
        var mine = await TestApi.Json(await api.Get("checks/demo/views/my-place", personId: "p1"));
        mine.GetProperty("status").GetString().ShouldBe("in");
        mine.GetProperty("name").GetString().ShouldBe("Alma Secretname");

        var other = await api.Get("checks/demo/views/my-place", personId: "p2");
        var text = await other.Content.ReadAsStringAsync();
        text.ShouldNotContain("Alma");
        JsonDocument.Parse(text).RootElement.GetProperty("status").GetString().ShouldBe("can-join");

        var nobody = await TestApi.Json(await api.Get("checks/demo/views/my-place"));
        nobody.GetProperty("status").GetString().ShouldBe("can-join");
        var bad = await TestApi.Json(await api.Get("checks/demo/views/my-place", personId: "../x y"));
        bad.GetProperty("status").GetString().ShouldBe("can-join");
    }

    [Fact]
    public async Task Owner_views_answer_the_owner_with_the_right_shape_and_never_show_the_key()
    {
        var (api, key) = await Started();
        await using var _a = api;
        foreach (var v in OwnerViews)
        {
            var res = await api.Get($"checks/demo/views/{v}", key);
            res.StatusCode.ShouldBe(HttpStatusCode.OK, v);
            var text = await res.Content.ReadAsStringAsync();
            text.ShouldNotContain(key, Case.Sensitive, v);
            text.ShouldNotContain("ownerKeyHash", Case.Sensitive, v);
            text.ShouldNotContain("p1", Case.Sensitive, v);
        }
        var progress = await TestApi.Json(await api.Get("checks/demo/views/owner-progress", key));
        progress.GetProperty("toGo").GetInt32().ShouldBe(3);
        var reach = await TestApi.Json(await api.Get("checks/demo/views/reachable-people", key));
        reach.GetProperty("people")[0].GetProperty("email").GetString().ShouldBe("alma@example.com");
        var who = await TestApi.Json(await api.Get("checks/demo/views/whos-in", key));
        who.GetProperty("people")[0].GetProperty("leftEmail").GetBoolean().ShouldBeTrue();
        (await TestApi.Json(await api.Get("checks/demo/views/close-state", key))).GetProperty("status").GetString().ShouldBe("open");
        (await TestApi.Json(await api.Get("checks/demo/views/closing-date", key))).GetProperty("canChange").GetBoolean().ShouldBeTrue();
        (await TestApi.Json(await api.Get("checks/demo/views/signups-per-day", key))).GetProperty("days").GetArrayLength().ShouldBe(7);
    }

    [Fact]
    public async Task Owner_views_are_403_without_a_key_or_with_a_wrong_one()
    {
        var (api, _) = await Started();
        await using var _a = api;
        var other = await api.StartCheck("other");
        foreach (var v in OwnerViews)
            foreach (var sent in new string?[] { null, "wrong", other })
            {
                var res = await api.Get($"checks/demo/views/{v}", sent);
                res.StatusCode.ShouldBe(HttpStatusCode.Forbidden, v);
                var text = await res.Content.ReadAsStringAsync();
                (await TestApi.Error(res)).ShouldBe("Only the owner can see this");
                text.ShouldNotContain("Alma");
            }
    }

    [Fact]
    public async Task A_missing_check_is_404_and_a_bad_id_is_400()
    {
        await using var api = await TestApi.Start();
        foreach (var v in PublicViews.Concat(OwnerViews))
        {
            (await api.Get($"checks/nope/views/{v}", "k")).StatusCode.ShouldBe(HttpStatusCode.NotFound, v);
            var bad = await api.Get($"checks/bad.id/views/{v}", "k");
            bad.StatusCode.ShouldBe(HttpStatusCode.BadRequest, v);
            (await TestApi.Error(bad)).ShouldBe("invalid checkId");
        }
        (await TestApi.Error(await api.Get("checks/nope/views/pitch-card"))).ShouldBe("No such interest check");
    }

    [Fact]
    public async Task My_interest_checks_lists_checks_with_good_keys_newest_first_and_skips_the_rest()
    {
        await using var api = await TestApi.Start();
        var k1 = await api.StartCheck("one");
        await Task.Delay(20);
        var k2 = await api.StartCheck("two");
        await api.Join("two", "p1", "Alma");

        var res = await api.Post("views/my-interest-checks", new
        {
            links = new object[]
            {
                new { checkId = "one", ownerKey = k1 }, new { checkId = "two", ownerKey = k2 },
                new { checkId = "nope", ownerKey = "x" }, new { checkId = "bad.id", ownerKey = "x" },
            },
        });
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = await res.Content.ReadAsStringAsync();
        text.ShouldNotContain(k1);
        text.ShouldNotContain("Alma");
        var checks = JsonDocument.Parse(text).RootElement.GetProperty("checks").EnumerateArray().ToList();
        checks.Select(c => c.GetProperty("checkId").GetString()).ShouldBe(["two", "one"]);
        checks[0].GetProperty("count").GetInt32().ShouldBe(1);
        checks[0].GetProperty("open").GetBoolean().ShouldBeTrue();

        var wrong = await TestApi.Json(await api.Post("views/my-interest-checks", new { links = new[] { new { checkId = "one", ownerKey = k2 } } }));
        wrong.GetProperty("checks").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task My_interest_checks_takes_at_most_50_links()
    {
        await using var api = await TestApi.Start();
        var links = Enumerable.Range(0, 51).Select(i => new { checkId = $"c{i}", ownerKey = "k" }).ToArray();
        (await api.Post("views/my-interest-checks", new { links })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await api.Post("views/my-interest-checks", new { links = links.Take(50) })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
