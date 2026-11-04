using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class StartCheckTests
{
    [Fact]
    public async Task Starting_a_check_returns_the_id_and_a_long_url_safe_owner_key()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks", new { checkId = "demo", title = "Board games", pitch = "Fridays", goal = 4, closesOn = TestApi.Tomorrow });
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await TestApi.Json(res);
        body.GetProperty("checkId").GetString().ShouldBe("demo");
        var key = body.GetProperty("ownerKey").GetString()!;
        key.Length.ShouldBeGreaterThanOrEqualTo(24);
        key.ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    [Fact]
    public async Task Two_checks_get_different_keys()
    {
        await using var api = await TestApi.Start();
        (await api.StartCheck("a")).ShouldNotBe(await api.StartCheck("b"));
    }

    [Fact]
    public async Task The_key_is_never_stored_only_its_sha256_hash()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        var raw = await api.RawStream("demo");
        raw.ShouldNotContain(key);
        raw.ShouldContain(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key))).ToLowerInvariant());
        raw.ShouldContain("InterestCheckStarted");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_title_is_refused(string title)
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks", new { checkId = "demo", title, goal = 3 });
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApi.Error(res)).ShouldBe("What's the idea called?");
    }

    [Fact]
    public async Task Title_length_is_bounded_at_120()
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId = "a", title = new string('x', 120), goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.Post("checks", new { checkId = "b", title = new string('x', 121), goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Pitch_length_is_bounded_at_1000()
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId = "a", title = "t", pitch = new string('x', 1000), goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.Post("checks", new { checkId = "b", title = "t", pitch = new string('x', 1001), goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100001)]
    [InlineData(2.5)]
    [InlineData("3")]
    [InlineData(null)]
    public async Task A_goal_that_is_not_an_integer_from_1_to_100000_is_refused(object? goal)
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks", new { checkId = "demo", title = "t", goal });
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    public async Task The_goal_bounds_are_accepted(int goal)
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId = "demo", title = "t", goal })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("tomorrow")]
    [InlineData("2026/12/31")]
    [InlineData("31-12-2026")]
    [InlineData("2026-13-01")]
    public async Task A_closing_date_that_is_not_yyyy_MM_dd_is_refused(string closesOn)
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId = "demo", title = "t", goal = 3, closesOn })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_closing_date_in_the_past_is_refused_but_today_is_fine()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks", new { checkId = "a", title = "t", goal = 3, closesOn = TestApi.Yesterday });
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApi.Error(res)).ShouldBe("The closing date has already passed");
        (await api.Post("checks", new { checkId = "b", title = "t", goal = 3, closesOn = DateTime.UtcNow.ToString("yyyy-MM-dd") })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../etc")]
    [InlineData("a/b")]
    [InlineData("with space")]
    public async Task A_checkId_that_is_not_a_short_url_safe_id_is_refused(string checkId)
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId, title = "t", goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_too_long_checkId_is_refused()
    {
        await using var api = await TestApi.Start();
        (await api.Post("checks", new { checkId = new string('a', 65), title = "t", goal = 3 })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Starting_a_check_that_already_exists_is_a_conflict_and_keeps_the_first_key()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        var res = await api.Post("checks", new { checkId = "demo", title = "Other", goal = 9 });
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("This interest check already exists");
        (await api.Get("checks/demo/owner-events", key)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Simultaneous_starts_of_the_same_check_let_exactly_one_through()
    {
        await using var api = await TestApi.Start();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => api.Post("checks", new { checkId = "race", title = "t", goal = 3 })));
        results.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(7);
    }
}
