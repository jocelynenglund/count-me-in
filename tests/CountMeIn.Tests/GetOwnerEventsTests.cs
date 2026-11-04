using System.Net;
using System.Text.Json;
using Shouldly;

namespace CountMeIn.Tests;

public class GetOwnerEventsTests
{
    [Fact]
    public async Task The_owner_sees_names_emails_and_person_ids_but_never_the_key_hash()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Join("demo", "p1", "Alma", "alma@example.com");
        await api.Join("demo", "p2", "Bo", null);

        var res = await api.Get("checks/demo/owner-events", key);
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = await res.Content.ReadAsStringAsync();
        text.ShouldNotContain("ownerKeyHash");
        text.ShouldNotContain(key);

        var events = JsonDocument.Parse(text).RootElement.EnumerateArray().ToList();
        events.Count.ShouldBe(3);
        events[0].GetProperty("type").GetString().ShouldBe("InterestCheckStarted");
        events[1].GetProperty("name").GetString().ShouldBe("Alma");
        events[1].GetProperty("email").GetString().ShouldBe("alma@example.com");
        events[1].GetProperty("personId").GetString().ShouldBe("p1");
        events[1].GetProperty("person").GetString()!.Length.ShouldBe(16);
        events[1].TryGetProperty("at", out _).ShouldBeTrue();
        events[2].TryGetProperty("email", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task A_missing_key_is_403()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Get("checks/demo/owner-events");
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TestApi.Error(res)).ShouldBe("Only the owner can see this");
    }

    [Fact]
    public async Task A_wrong_key_is_403()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        var res = await api.Get("checks/demo/owner-events", "not-the-key");
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).ShouldNotContain("Alma");
    }

    [Fact]
    public async Task The_key_of_another_check_does_not_open_this_one()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("one");
        var other = await api.StartCheck("two");
        (await api.Get("checks/one/owner-events", other)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_unknown_check_is_404()
    {
        await using var api = await TestApi.Start();
        (await api.Get("checks/nope/owner-events", "k")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
