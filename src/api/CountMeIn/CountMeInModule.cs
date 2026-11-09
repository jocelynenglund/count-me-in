using CountMeIn.Features.PickALook;
using CountMeIn.Features.ShowInterest;
using CountMeIn.Features.StartAnInterestCheck;
using CountMeIn.Features.WatchItGrow;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CountMeIn;

/// <summary>
/// Count me in: the store and the privacy guards. The browser runs the business
/// rules; this module keeps one stream per interest check and decides who may see
/// which fields of it.
/// </summary>
public class CountMeInModule : IModule
{
    public const string Prefix = "/api/countmein";

    /// <summary>The module name in storage. Add it to <c>FolderStoreModules</c> in the host's appsettings.json.</summary>
    public const string Module = "countmein";

    public string Name => "CountMeIn";

    public string RoutePrefix => Prefix;

    // Called straight from browsers; the privacy guards are the owner key and the field filtering.
    public bool RequiresApiKey => false;

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Nothing to register. The host constructs modules with Activator.CreateInstance.
    }

    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup(Prefix);

        StartInterestCheckEndpoint.Map(group);
        GetEvents.Map(group);
        GetOwnerEvents.Map(group);
        CountMeInEndpoint.Map(group);
        ChangeMyMindEndpoint.Map(group);
        CloseInterestCheckEndpoint.Map(group);
        ChooseThemeEndpoint.Map(group);
        ChangeClosingDateEndpoint.Map(group);
        LookChoices.Map(group);
        StartForm.Map(group);
        MyInterestChecks.Map(group);
        PitchCard.Map(group);
        PitchCount.Map(group);
        MyPlace.Map(group);
        PitchLook.Map(group);
        OwnerProgress.Map(group);
        SignupsPerDay.Map(group);
        WhosIn.Map(group);
        ReachablePeople.Map(group);
        ClosingDate.Map(group);
        CloseState.Map(group);
        // Which release the host runs: the web deploy waits for its tag here before it uploads the pages (API ships before web).
        group.MapGet("/version", () => Results.Ok(new { version = typeof(CountMeInModule).Assembly.GetName().Version?.ToString(3) }));
    }
}
