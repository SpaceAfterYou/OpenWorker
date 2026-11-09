using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Server.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// What the district and the maze share: the person of a player read from its row, and the audience of the server, which
    /// the shared handlers tell what a player does. The handlers themselves are found with the other handlers of the process
    /// (<c>AddHotspot</c>); calling this loads the assembly they are in.
    /// </summary>
    public static IServiceCollection AddServerCommon<TAudience>(this IServiceCollection services)
        where TAudience : class, IPlayerAudience
    {
        return services
            .AddSingleton<PersonRegistry>()
            .AddSingleton<IPlayerAudience, TAudience>();
    }
}
