using SoulWorkerResearch.SoulCore.IO.Net;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal sealed record CreatedContext(Opcode Opcode, HandlerMethod Method);
