using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class CloseCheckTests
{
    [Fact]
    public async Task The_owner_closes_the_check_with_an_empty_body()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        (await api.Post("checks/demo/close", null, key)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var last = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().Last();
        last.GetProperty("type").GetString().ShouldBe("InterestCheckClosed");
        last.GetProperty("on").GetString().ShouldBe(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task An_unknown_check_is_404()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks/nope/close", null, "k");
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_missing_key_is_403_and_closes_nothing()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Post("checks/demo/close");
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TestApi.Error(res)).ShouldBe("Only the owner can close this");
        (await api.Post("checks/demo/interest", new { personId = "p1", name = "Alma" })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_wrong_key_is_403()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        (await api.Post("checks/demo/close", null, "wrong")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_key_of_another_check_is_403()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("one");
        var other = await api.StartCheck("two");
        (await api.Post("checks/one/close", null, other)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Closing_twice_is_a_conflict()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Post("checks/demo/close", null, key);
        var res = await api.Post("checks/demo/close", null, key);
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("It's already closed");
    }
}
