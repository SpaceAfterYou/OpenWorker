using Microsoft.Extensions.Configuration;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Hotspot.Extensions;

internal static class ConfigurationExtensions
{
    internal static EntityComponentService GetTarget(this IConfiguration configuration)
    {
        return configuration.GetValue<EntityComponentService>(ConfigurationUtils.InstanceTargetKey);
    }
}