using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record ReachablePerson(string Name, string Email);
public sealed record ReachablePeopleView(IReadOnlyList<ReachablePerson> People);

/// <summary>Reachable People: those in now who left an email. Owner only.</summary>
public static class ReachablePeople
{
    public static ReachablePeopleView Project(IReadOnlyList<object> history) =>
        new(CheckFacts.From(history).People.Where(p => !string.IsNullOrEmpty(p.Email)).Select(p => new ReachablePerson(p.Name, p.Email!)).ToList());

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "reachable-people", (h, _) => Project(h));
}
