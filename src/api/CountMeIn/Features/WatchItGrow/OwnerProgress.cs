using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record OwnerProgressView(string Title, int Count, int Goal, int ToGo, string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, int MeterBoxes, int MeterOn);

public static class OwnerProgress
{
    public static OwnerProgressView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
