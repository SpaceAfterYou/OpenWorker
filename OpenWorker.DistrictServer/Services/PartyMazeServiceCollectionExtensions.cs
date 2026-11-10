using OpenWorker.Channel.Community;
using OpenWorker.Hotspot;

namespace OpenWorker.DistrictServer.Services;

public static class PartyMazeServiceCollectionExtensions
{
    /// <summary>
    /// A party enters a maze together (<see cref="PartyMazeEntry"/>); a member who leaves the district ends the question.
    /// </summary>
    public static IServiceCollection AddPartyMazeEntry(this IServiceCollection services)
    {
        services.AddSingleton<IPartyMazeRoutes, PartyMazeRoutes>();
        services.AddSingleton<PartyMazeEntry>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<PartyMazeEntry>());
        services.AddSingleton<IPartyMatchListener>(provider => provider.GetRequiredService<PartyMazeEntry>());
        services.AddSingleton<ICommunityModule>(provider => provider.GetRequiredService<PartyMazeEntry>());
        services.AddHostedService<PartyMazeQuestionService>();

        return services;
    }
}
