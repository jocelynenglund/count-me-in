using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record MyPlaceView(string Status, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

public static class MyPlace
{
    public static MyPlaceView Project(IReadOnlyList<object> history, string? personId, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
