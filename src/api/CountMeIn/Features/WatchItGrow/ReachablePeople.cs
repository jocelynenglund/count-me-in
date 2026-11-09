using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record ReachablePerson(string Name, string Email);
public sealed record ReachablePeopleView(IReadOnlyList<ReachablePerson> People);

public static class ReachablePeople
{
    public static ReachablePeopleView Project(IReadOnlyList<object> history) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
