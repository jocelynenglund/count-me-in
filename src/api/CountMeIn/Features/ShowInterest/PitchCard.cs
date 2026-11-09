using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record PitchCardView(string Title, string Pitch, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, bool ClosedByDate);

public static class PitchCard
{
    public static PitchCardView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
