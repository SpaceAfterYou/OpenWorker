using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler.Extensions;

public static class TypeExtension
{
    internal static bool IsHotspotHandler(this Type @this)
    {
        return !@this.IsAbstract && @this.GetInterfaces().Any(IsImplementInterface);
    }

    private static bool IsImplementInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHotspotHandler<>);
    }
}