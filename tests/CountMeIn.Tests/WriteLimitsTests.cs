using System.Net;
using CountMeIn.Store;
using Shouldly;

namespace CountMeIn.Tests;

public class WriteLimitsTests
{
    sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    static WriteLimits Tiny(int max = 2) => new(
        Enum.GetValues<WriteGroup>().ToDictionary(g => g, _ => new WriteLimits.Limit(max, TimeSpan.FromMinutes(1))), TimeProvider.System);

    static async Task Over(HttpResponseMessage res)
    {
        res.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await TestApi.Error(res)).ShouldBe("Too many tries. Wait a little and try again.");
        res.Headers.RetryAfter.ShouldNotBeNull();
        res.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Defaults_are_the_agreed_numbers()
    {
        WriteLimits.Defaults[WriteGroup.StartCheck].ShouldBe(new(10, TimeSpan.FromHours(1)));
        WriteLimits.Defaults[WriteGroup.CountInOrWithdraw].ShouldBe(new(60, TimeSpan.FromMinutes(10)));
        WriteLimits.Defaults[WriteGroup.OwnerCommand].ShouldBe(new(60, TimeSpan.FromMinutes(10)));
        WriteLimits.Defaults[WriteGroup.MyChecksView].ShouldBe(new(30, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void A_different_address_has_its_own_budget_and_the_window_resets()
    {
        var clock = new Clock();
        var l = new WriteLimits(Enum.GetValues<WriteGroup>().ToDictionary(g => g, _ => new WriteLimits.Limit(1, TimeSpan.FromMinutes(1))), clock);
        l.TryAcquire(WriteGroup.StartCheck, "1.1.1.1", out _).ShouldBeTrue();
        l.TryAcquire(WriteGroup.StartCheck, "1.1.1.1", out var retry).ShouldBeFalse();
        retry.ShouldBe(60);
        l.TryAcquire(WriteGroup.StartCheck, "2.2.2.2", out _).ShouldBeTrue();
        l.TryAcquire(WriteGroup.OwnerCommand, "1.1.1.1", out _).ShouldBeTrue();
        clock.Now += TimeSpan.FromMinutes(1);
        l.TryAcquire(WriteGroup.StartCheck, "1.1.1.1", out _).ShouldBeTrue();
    }

    [Fact]
    public void Memory_stays_bounded_under_a_flood_of_addresses()
    {
        var clock = new Clock();
        var l = new WriteLimits(WriteLimits.Defaults, clock);
        for (var i = 0; i < 30000; i++) l.TryAcquire(WriteGroup.MyChecksView, "ip" + i, out _);
        clock.Now += TimeSpan.FromHours(2);
        l.TryAcquire(WriteGroup.MyChecksView, "again", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Starting_checks_stops_after_the_limit_but_reads_do_not()
    {
        await using var api = await TestApi.Start(Tiny());
        await api.StartCheck("a");
        await api.StartCheck("b");
        await Over(await api.Post("checks", new { checkId = "c", title = "T", goal = 3 }));
        for (var i = 0; i < 5; i++) (await api.Get("checks/a/events")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Counting_in_and_withdrawing_share_one_budget()
    {
        await using var api = await TestApi.Start(Tiny());
        await api.StartCheck("demo"); // start has its own budget
        await api.Join("demo", "p1");
        (await api.Post("checks/demo/withdraw", new { personId = "p1" })).StatusCode.ShouldBe(HttpStatusCode.OK);
        await Over(await api.Post("checks/demo/interest", new { personId = "p2", name = "Bo" }));
        await Over(await api.Post("checks/demo/withdraw", new { personId = "p1" }));
        (await api.Get("checks/demo/events")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Owner_commands_share_one_budget()
    {
        await using var api = await TestApi.Start(Tiny());
        var key = await api.StartCheck("demo");
        (await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Tomorrow }, key)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.Post("checks/demo/theme", new { theme = "christmas" }, key)).StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        await Over(await api.Post("checks/demo/close", null, key));
        await Over(await api.Post("checks/demo/closing-date", new { closesOn = TestApi.Tomorrow }, key));
    }

    [Fact]
    public async Task My_checks_view_stops_after_the_limit()
    {
        await using var api = await TestApi.Start(Tiny());
        for (var i = 0; i < 2; i++)
            (await api.Post("views/my-interest-checks", new { links = Array.Empty<object>() })).StatusCode.ShouldBe(HttpStatusCode.OK);
        await Over(await api.Post("views/my-interest-checks", new { links = Array.Empty<object>() }));
    }
}
