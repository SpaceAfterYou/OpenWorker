using SoulWorkerResearch.SoulCore.IO.Net;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal sealed record CreatedHandler(Opcode Opcode, Type Class, HandlerMethod Method);
