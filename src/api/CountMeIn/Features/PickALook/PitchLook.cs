using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchLookView(string CheckId, string Theme);

public static class PitchLook
{
    public static PitchLookView Project(IReadOnlyList<object> history) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
