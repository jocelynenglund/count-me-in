using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchCountView(int Count, int Goal, bool Open, int MeterBoxes, int MeterOn);

public static class PitchCount
{
    public static PitchCountView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
