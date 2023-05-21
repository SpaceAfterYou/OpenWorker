using DefaultEcs;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal delegate ValueTask HandlerMethod(object instance, Entity entity, BinaryReader reader, CancellationToken ct);
