using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record ClosingDateView([property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClosesOn, bool CanChange, string Earliest);

public static class ClosingDate
{
    public static ClosingDateView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
