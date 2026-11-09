using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record MyCheckLink(string CheckId, string OwnerKey);
public sealed record MyCheck(string CheckId, string Title, int Count, int Goal, bool Open);
public sealed record MyInterestChecksView(IReadOnlyList<MyCheck> Checks);

public static class MyInterestChecks
{
    public static MyInterestChecksView Project(IReadOnlyList<object> history, IReadOnlyList<MyCheckLink> links, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
