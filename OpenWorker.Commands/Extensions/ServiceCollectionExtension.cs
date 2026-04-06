using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Commands.Extensions;

public static partial class ServiceCollectionExtension
{
    public static void AddCommandManager(this IServiceCollection services)
    {
        services.AddCommands();
        services.AddSingleton<CommandManager>();
    }
}