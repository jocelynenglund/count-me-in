using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record StartFormView(string EarliestClosingDate, int DefaultGoal);

/// <summary>Start Form: where the form on the start page begins (no check involved).</summary>
public static class StartForm
{
    public const int DefaultGoal = 10;

    public static StartFormView Project(IReadOnlyList<object> history, string today) => new(today, DefaultGoal);

    public static void Map(RouteGroupBuilder g) => g.MapGet("/views/start-form", () => Results.Ok(Project([], Streams.Today())));
}
