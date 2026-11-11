using OpenWorker.Channel.Community;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Server.Common.Services;

namespace OpenWorker.Server.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// What the district and the maze share: the person of a player read from its row, the audience of the server, which
    /// the shared handlers tell what a player does, and the titles of a person for the quests that reward them. The handlers
    /// themselves are found with the other handlers of the process (<c>AddHotspot</c>); calling this loads the assembly they
    /// are in.
    /// </summary>
    public static IServiceCollection AddServerCommon<TAudience>(this IServiceCollection services)
        where TAudience : class, IPlayerAudience
    {
        services.Replace(ServiceDescriptor.Singleton<IQuestTitles, QuestTitles>());
        services.Replace(ServiceDescriptor.Singleton<IItemTitles, ItemTitles>());

        return services
            .AddSingleton<PersonRegistry>()
            .AddSingleton<HelperSupports>()
            .AddSingleton<CommandAccess>()
            .AddSingleton<SkillInputRelay>()
            .AddHostedService<KeepAliveService>()
            .AddSingleton<IPlayerAudience, TAudience>();
    }
}
