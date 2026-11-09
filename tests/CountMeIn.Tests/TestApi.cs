using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CountMeIn.Tests;

/// <summary>The module mapped into a real app on a loopback port with a temp-dir store: no Docker, no outside network.</summary>
public sealed class TestApi : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly DirectoryInfo _data = Directory.CreateTempSubdirectory("countmein-tests-");
    public HttpClient Http { get; }
    public FolderEventStore Store { get; }

    private TestApi(WebApplication app, FolderEventStore store, HttpClient http) { _app = app; Store = store; Http = http; }

    public static async Task<TestApi> Start()
    {
        var data = Directory.CreateTempSubdirectory("countmein-store-");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var store = new FolderEventStore(data.FullName);
        builder.Services.AddSingleton<IEventStore>(store);
        builder.Services.AddSingleton<ILockingEventStore>(store);
        var module = new CountMeInModule();
        module.ConfigureServices(builder.Services, builder.Configuration);
        var app = builder.Build();
        module.MapEndpoints(app);
        await app.StartAsync();
        var http = new HttpClient { BaseAddress = new Uri(app.Urls.First() + CountMeInModule.Prefix + "/") };
        var api = new TestApi(app, store, http);
        api._storeDir = data;
        return api;
    }

    private DirectoryInfo? _storeDir;

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await _app.DisposeAsync();
        try { _data.Delete(true); } catch { }
        try { _storeDir?.Delete(true); } catch { }
    }

    public static string Tomorrow => DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
    public static string Yesterday => DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

    public Task<HttpResponseMessage> Post(string path, object? body = null, string? ownerKey = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null) req.Content = JsonContent.Create(body);
        if (ownerKey is not null) req.Headers.Add("X-Owner-Key", ownerKey);
        return Http.SendAsync(req);
    }

    public Task<HttpResponseMessage> Get(string path, string? ownerKey = null, string? personId = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        if (ownerKey is not null) req.Headers.Add("X-Owner-Key", ownerKey);
        if (personId is not null) req.Headers.Add("X-Person-Id", personId);
        return Http.SendAsync(req);
    }

    /// <summary>Starts a check and returns its owner key.</summary>
    public async Task<string> StartCheck(string checkId = "demo", object? body = null)
    {
        var res = await Post("checks", body ?? new { checkId, title = "A board game night", pitch = "Fridays?", goal = 4 });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await Json(res)).GetProperty("ownerKey").GetString()!;
    }

    public async Task Join(string checkId, string personId, string name = "Alma", string? email = "alma@example.com")
    {
        var res = await Post($"checks/{checkId}/interest", new { personId, name, email });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    public static async Task<JsonElement> Json(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    public static async Task<string> Error(HttpResponseMessage res) => (await Json(res)).GetProperty("error").GetString()!;

    public Task<string> RawStream(string checkId) =>
        Task.Run(() => string.Join("\n", Directory.EnumerateFiles(_storeDir!.FullName, "*", SearchOption.AllDirectories)
            .Where(f => f.Contains($"check-{checkId}"))
            .Select(File.ReadAllText)));
}
