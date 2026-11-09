using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record CloseStateView(string Status);

public static class CloseState
{
    public static CloseStateView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
