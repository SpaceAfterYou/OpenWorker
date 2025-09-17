using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Commands;
using OpenWorker.Hotspot.Commands.Attributes;

namespace OpenWorker.Hotspot.Commands.Extensions;

public static class ServiceCollectionExtension
{
    public static void AddStuffCommands(this IServiceCollection services)
    {
        foreach (var command in GetCommandTypeList())
        {
            services.AddSingleton(command);
        }

        services.AddSingleton(provider =>
        {
            var commands = GetCommandTypeList()
                .Select(t => CreateCommand(t, provider))
                .ToDictionary();
        
            return new StuffCommands(commands);
        });
    }

    private static (string, AStuffCommand) CreateCommand(Type t, IServiceProvider provider)
    {
        var instance = provider.GetService(t) as AStuffCommand;
        Debug.Assert(instance is not null);

        var attribute = t.GetCustomAttribute<StuffCommandAttribute>();
        Debug.Assert(attribute is not null);

        return ($"/{attribute.Trigger}", instance);
    }

    private static IEnumerable<Type> GetCommandTypeList()
    {
        var assembly = typeof(AStuffCommand).Assembly;
        return assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(AStuffCommand)) && t.IsAbstract is false);
    }
}