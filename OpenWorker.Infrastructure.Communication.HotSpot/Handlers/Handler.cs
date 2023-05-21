using DefaultEcs;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal sealed record Handler(Type Class, HandlerMethod Method);

// https://youtu.be/ZJrEXthnycE
