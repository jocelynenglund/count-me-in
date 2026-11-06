using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class ChangeClosingDateTests
{
    [Fact]
    public async Task The_owner_moves_the_deadline_and_everyone_sees_it()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        (await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Tomorrow }, key)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var last = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().Last();
        last.GetProperty("type").GetString().ShouldBe("ClosingDateChanged");
        last.GetProperty("closesOn").GetString().ShouldBe(TestApi.Tomorrow);
    }

    [Fact]
    public async Task No_date_removes_the_deadline()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        (await api.Post("checks/demo/closing-date", new { closesOn = (string?)null }, key)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var last = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().Last();
        last.GetProperty("type").GetString().ShouldBe("ClosingDateChanged");
        last.TryGetProperty("closesOn", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Only_the_owner_can_change_the_date()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Tomorrow }, "someone-else");
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TestApi.Error(res)).ShouldBe("Only the person who started it can change its date.");
    }

    [Fact]
    public async Task Not_a_date_in_the_past()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        var res = await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Yesterday }, key);
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApi.Error(res)).ShouldBe("Pick a date from today on.");
    }

    [Fact]
    public async Task A_check_you_closed_stays_closed()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Post("checks/demo/close", null, key);
        var res = await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Tomorrow }, key);
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("You closed this one.");
    }

    [Fact]
    public async Task No_such_check_is_404()
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks/nope/closing-date", new { closesOn = TestApi.Tomorrow }, "k")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
