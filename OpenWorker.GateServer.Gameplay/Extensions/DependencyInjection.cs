using OpenWorker.Hotspot;
using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.GateServer.Gameplay.Extensions;

public static class DependencyInjection
{
    public static void AddGameplay(this IServiceCollection services)
    {
        services.AddSingleton<LoginGameplay>();
        services.AddSingleton<PersonCreationCatalog>();
        services.AddSingleton<PersonGameplay>();
        services.AddSingleton<OptionGameplay>();
        services.AddSingleton<SecondPasswordGameplay>();
        
        services.AddSingleton<PersonRegistry>();
        services.AddSingleton<ISessionCloseHandler>(e => e.GetRequiredService<PersonRegistry>());
    }
}