using Arch.Core;
using OpenWorker.Hotspot.Handler.DataTypes;

namespace OpenWorker.Gameplay;

/// <summary>Unpacks the boxed player handle from Hotspot dispatch (Arch <see cref="Entity"/>).</summary>
public static class ServiceHandleContextExtensions
{
    public static Entity GetPlayerEntity(this ServiceHandleContext context) => (Entity)context.Player!;
}
