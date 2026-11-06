using System.Net;
using Shouldly;

namespace CountMeIn.Tests;

/// <summary>GET /version: which release the host runs. The web deploy waits for its tag here (API ships before web).</summary>
public class VersionTests
{
    [Fact]
    public async Task The_host_says_which_release_it_runs()
    {
        await using var api = await TestApi.Start();
        var res = await api.Get("version");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await TestApi.Json(res)).GetProperty("version").GetString().ShouldMatch(@"^\d+\.\d+\.\d+$");
    }
}
