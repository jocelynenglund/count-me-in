using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record LookChoice(string Theme, string Label, bool Picked);
public sealed record LookChoicesView(IReadOnlyList<LookChoice> Choices);

public static class LookChoices
{
    public static LookChoicesView Project(IReadOnlyList<object> history) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
