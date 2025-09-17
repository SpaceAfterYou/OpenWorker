using OpenWorker.Hotspot.Handler.DataTypes;

namespace OpenWorker.Hotspot.Handler.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HotspotHandlerAttribute(HotspotHandlerType type) : Attribute
{
    public HotspotHandlerType Type { get; } = type;
}