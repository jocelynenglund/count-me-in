using System.Net;
using System.Text.Json;
using Shouldly;

namespace CountMeIn.Tests;

public class GetEventsTests
{
    [Fact]
    public async Task An_unknown_check_is_404()
    {
        await using var api = await TestApi.Start();
        var res = await api.Get("checks/nope/events");
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TestApi.Error(res)).ShouldBe("No such interest check");
    }

    [Fact]
    public async Task Events_come_back_flat_in_stream_order_with_a_timestamp()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo", new { checkId = "demo", title = "Board games", pitch = "Fridays", goal = 4, closesOn = TestApi.Tomorrow });
        await api.Join("demo", "p1");
        (await api.Post("checks/demo/withdraw", new { personId = "p1" })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.Post("checks/demo/close", null, key)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var events = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().ToList();
        events.Select(e => e.GetProperty("type").GetString()).ShouldBe(["InterestCheckStarted", "InterestShown", "InterestWithdrawn", "InterestCheckClosed"]);
        events.ShouldAllBe(e => !string.IsNullOrEmpty(e.GetProperty("at").GetString()));

        var started = events[0];
        started.GetProperty("title").GetString().ShouldBe("Board games");
        started.GetProperty("pitch").GetString().ShouldBe("Fridays");
        started.GetProperty("goal").GetInt32().ShouldBe(4);
        started.GetProperty("closesOn").GetString().ShouldBe(TestApi.Tomorrow);
        events[1].GetProperty("person").GetString()!.Length.ShouldBe(16);
        events[1].GetProperty("on").GetString().ShouldBe(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task Nothing_private_leaves_through_the_public_endpoint()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Join("demo", "secret-person-id", "Alma Secretname", "alma@secret.example");
        await api.Post("checks/demo/withdraw", new { personId = "secret-person-id" });
        await api.Post("checks/demo/close", null, key);

        var res = await api.Get("checks/demo/events");
        var text = await res.Content.ReadAsStringAsync();
        foreach (var forbidden in new[] { "Secretname", "alma@secret.example", "secret-person-id", "personId", "name", "email", "ownerKeyHash", key })
            text.ShouldNotContain(forbidden, Case.Sensitive);

        var allowed = new Dictionary<string, string[]>
        {
            ["InterestCheckStarted"] = ["type", "checkId", "title", "pitch", "goal", "closesOn", "at"],
            ["InterestShown"] = ["type", "checkId", "person", "on", "at"],
            ["InterestWithdrawn"] = ["type", "checkId", "person", "on", "at"],
            ["InterestCheckClosed"] = ["type", "checkId", "on", "at"],
        };
        foreach (var e in JsonDocument.Parse(text).RootElement.EnumerateArray())
            e.EnumerateObject().Select(p => p.Name).ShouldBeSubsetOf(allowed[e.GetProperty("type").GetString()!]);
    }

    [Fact]
    public async Task The_person_handle_is_the_same_for_a_join_and_its_withdrawal_but_differs_per_person()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        await api.Join("demo", "p2", "Bo", null);
        await api.Post("checks/demo/withdraw", new { personId = "p1" });
        var e = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().ToList();
        e[1].GetProperty("person").GetString().ShouldBe(e[3].GetProperty("person").GetString());
        e[1].GetProperty("person").GetString().ShouldNotBe(e[2].GetProperty("person").GetString());
    }

    [Fact]
    public async Task An_invalid_checkId_is_400()
    {
        await using var api = await TestApi.Start();
        (await api.Get("checks/bad.id/events")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
