using Arch.Core;

namespace OpenWorker.Hotspot.Handler.DataTypes;

public readonly record struct ServiceHandleContext(Entity Player, CancellationToken CancellationToken);
