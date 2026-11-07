using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;

namespace OpenWorker.DependencyInjection;

public sealed class ArchComponentProvider(IConfiguration configuration, ILogger<ArchComponentProvider> logger)
{
    private ComponentType[] Components { get; } = PrivateGetComponents(configuration, logger);

    private object[] Defaults => field ??= Components.Select(x => Activator.CreateInstance(x.Type)!).ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentType[] GetComponents() => Components;

    /// <summary>
    /// Creates a player entity with default components. World.Create reuses the memory of
    /// destroyed entities without clearing it, so a new connection would otherwise inherit
    /// the state of a previous player, including its login.
    /// </summary>
    public Entity CreateEntity(World world)
    {
        var entity = world.Create(Components);

        world.SetRange(entity, Defaults);

        return entity;
    }

    private static ComponentType[] PrivateGetComponents(
        IConfiguration configuration,
        ILogger<ArchComponentProvider> logger)
    {
        var target = configuration.GetValue<EntityComponentService>(ConfigurationUtils.InstanceTargetKey);
        Debug.Assert(target != EntityComponentService.All);

        logger.LogDebug("Getting components for {Target}", target);

        var values = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(type =>
            {
                var attribute = type
                    .GetCustomAttributes<EntityComponentAttribute>()
                    .ToArray();

                return
                    attribute.Length != 0 &&
                    attribute.Any(x => x.Service == EntityComponentService.All || x.Service == target);
            })
            .Select(Component.GetComponentType)
            .ToArray();

        foreach (var value in values)
        {
            logger.LogDebug("Found component: {Name}", value.Type.Name);
        }

        return values;
    }
}
