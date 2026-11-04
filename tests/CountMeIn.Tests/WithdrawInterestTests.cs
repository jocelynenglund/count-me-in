using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class WithdrawInterestTests
{
    [Fact]
    public async Task Withdrawing_records_the_same_person_handle_and_the_servers_date()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Join("demo", "p1");
        (await api.Post("checks/demo/withdraw", new { personId = "p1" })).StatusCode.ShouldBe(HttpStatusCode.OK);

        var last = (await TestApi.Json(await api.Get("checks/demo/owner-events", key))).EnumerateArray().Last();
        last.GetProperty("type").GetString().ShouldBe("InterestWithdrawn");
        last.GetProperty("personId").GetString().ShouldBe("p1");
        last.GetProperty("person").GetString().ShouldBe(CountMeIn.Features.Checks.Streams.PersonHandle("demo", "p1"));
        last.GetProperty("on").GetString().ShouldBe(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task An_unknown_check_is_404()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks/nope/withdraw", new { personId = "p1" });
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TestApi.Error(res)).ShouldBe("No such interest check");
    }

    [Fact]
    public async Task Withdrawing_when_not_counted_in_is_a_conflict()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Post("checks/demo/withdraw", new { personId = "p1" });
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("You're not counted in");
    }

    [Fact]
    public async Task Withdrawing_twice_is_a_conflict()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        await api.Post("checks/demo/withdraw", new { personId = "p1" });
        (await api.Post("checks/demo/withdraw", new { personId = "p1" })).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Someone_elses_personId_cannot_be_withdrawn_by_a_stranger()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        (await api.Post("checks/demo/withdraw", new { personId = "p2" })).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdrawing_still_works_after_the_check_is_closed()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Join("demo", "p1");
        await api.Post("checks/demo/close", null, key);
        (await api.Post("checks/demo/withdraw", new { personId = "p1" })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
