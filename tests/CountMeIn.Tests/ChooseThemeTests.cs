using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class ChooseThemeTests
{
    [Fact]
    public async Task The_owner_turns_it_Christmas_and_everyone_sees_it()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        (await api.Post("checks/demo/theme", new { theme = "christmas" }, key)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var last = (await TestApi.Json(await api.Get("checks/demo/events"))).EnumerateArray().Last();
        last.GetProperty("type").GetString().ShouldBe("ThemeChosen");
        last.GetProperty("theme").GetString().ShouldBe("christmas");
    }

    [Fact]
    public async Task Only_the_owner_can_change_the_look()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Post("checks/demo/theme", new { theme = "christmas" }, "someone-else");
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TestApi.Error(res)).ShouldBe("Only the person who started it can change its look.");
    }

    [Fact]
    public async Task Only_the_themes_there_are()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        var res = await api.Post("checks/demo/theme", new { theme = "neon" }, key);
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApi.Error(res)).ShouldBe("Pick one of the themes.");
    }

    [Fact]
    public async Task No_such_check_is_404()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks/nope/theme", new { theme = "christmas" }, "k");
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TestApi.Error(res)).ShouldBe("There is no such interest check.");
    }

    [Fact]
    public async Task A_closed_check_can_still_change_its_look()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Post("checks/demo/close", null, key);
        (await api.Post("checks/demo/theme", new { theme = "christmas" }, key)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
