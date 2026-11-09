using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record SignupDay(string Day, int In);
public sealed record SignupsPerDayView(IReadOnlyList<SignupDay> Days);

public static class SignupsPerDay
{
    public static SignupsPerDayView Project(IReadOnlyList<object> history, string today) => throw new NotImplementedException();
    public static void Map(RouteGroupBuilder g) { }
}
