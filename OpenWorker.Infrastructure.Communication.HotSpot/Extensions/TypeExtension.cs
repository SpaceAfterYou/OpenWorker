using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Extensions;

public static class TypeExtension
{
    internal static bool IsPacketHandler(this Type @this) => @this.GetInterfaces().Any(IsImplementIPacketHandler);

    private static bool IsImplementIPacketHandler(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHotSpotHandler<>);
}
