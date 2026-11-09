using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

public class ShowInterestTests
{
    [Fact]
    public async Task Counting_in_stores_the_name_and_email_with_the_servers_date()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        // A client-supplied date is ignored.
        var res = await api.Post("checks/demo/interest", new { personId = "p1", name = "  Alma ", email = "alma@example.com", on = "1999-01-01" });
        res.StatusCode.ShouldBe(HttpStatusCode.OK);

        var shown = (await TestApi.Json(await api.Get("checks/demo/owner-events", key))).EnumerateArray().Last();
        shown.GetProperty("type").GetString().ShouldBe("InterestShown");
        shown.GetProperty("name").GetString().ShouldBe("Alma");
        shown.GetProperty("on").GetString().ShouldBe(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        shown.GetProperty("person").GetString().ShouldBe(CountMeIn.Domain.Ids.PersonHandle("demo", "p1"));
        shown.GetProperty("checkId").GetString().ShouldBe("demo");
    }

    [Fact]
    public async Task An_unknown_check_is_404()
    {
        await using var api = await TestApi.Start();
        var res = await api.Post("checks/nope/interest", new { personId = "p1", name = "Alma" });
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TestApi.Error(res)).ShouldBe("No such interest check");
    }

    [Fact]
    public async Task A_closed_check_is_a_conflict()
    {
        await using var api = await TestApi.Start();
        var key = await api.StartCheck("demo");
        await api.Post("checks/demo/close", null, key);
        var res = await api.Post("checks/demo/interest", new { personId = "p1", name = "Alma" });
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("This interest check has closed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task An_empty_name_is_refused(string? name)
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var res = await api.Post("checks/demo/interest", new { personId = "p1", name });
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApi.Error(res)).ShouldBe("What's your name?");
    }

    [Fact]
    public async Task Name_and_email_lengths_are_bounded()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        (await api.Post("checks/demo/interest", new { personId = "a", name = new string('n', 61) })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await api.Post("checks/demo/interest", new { personId = "b", name = "ok", email = new string('e', 201) })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await api.Post("checks/demo/interest", new { personId = "c", name = new string('n', 60), email = new string('e', 200) })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../x")]
    [InlineData("has space")]
    public async Task A_personId_that_is_not_url_safe_is_refused(string personId)
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        (await api.Post("checks/demo/interest", new { personId, name = "Alma" })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_personId_over_64_characters_is_refused()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        (await api.Post("checks/demo/interest", new { personId = new string('p', 65), name = "Alma" })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Counting_in_twice_is_a_conflict()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        var res = await api.Post("checks/demo/interest", new { personId = "p1", name = "Alma" });
        res.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApi.Error(res)).ShouldBe("You're already counted in");
    }

    [Fact]
    public async Task After_withdrawing_you_can_count_in_again()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        await api.Join("demo", "p1");
        await api.Post("checks/demo/withdraw", new { personId = "p1" });
        (await api.Post("checks/demo/interest", new { personId = "p1", name = "Alma" })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Simultaneous_taps_from_one_device_count_once()
    {
        await using var api = await TestApi.Start();
        await api.StartCheck("demo");
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => api.Post("checks/demo/interest", new { personId = "p1", name = "Alma" })));
        results.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(7);
    }
}
