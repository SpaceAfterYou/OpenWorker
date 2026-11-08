using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[WireMessage]
public readonly partial record struct ChatMemoryStatisticsValue(int Count, int Size);