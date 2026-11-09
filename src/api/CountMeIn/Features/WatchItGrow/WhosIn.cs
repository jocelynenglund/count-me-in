using System.Text.Json.Serialization;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CountMeIn.Features.Checks;

public sealed record WhoIsIn(string Name, bool LeftEmail);
public sealed record WhosInView(IReadOnlyList<WhoIsIn> People);

/// <summary>Who's In: who is in now, and whether they left an email (not the email). Owner only.</summary>
public static class WhosIn
{
    public static WhosInView Project(IReadOnlyList<object> history) =>
        new(CheckFacts.From(history).People.Select(p => new WhoIsIn(p.Name, !string.IsNullOrEmpty(p.Email))).ToList());

    public static void Map(RouteGroupBuilder g) => ViewRoutes.Owner(g, "whos-in", (h, _) => Project(h));
}
