using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record WhoIsIn(string Name, bool LeftEmail);
public sealed record WhosInView(IReadOnlyList<WhoIsIn> People);

public static class WhosIn
{
    public static WhosInView Project(IReadOnlyList<object> history) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
