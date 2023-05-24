using SoulWorkerResearch.SoulCore.Defines;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Cache;

public sealed record ChannelModel
{
    public ushort District { get; init; }
    public IEnumerable<Entry> Entries { get; init; } = Enumerable.Empty<Entry>();

    public sealed record Entry
    {
        public ushort Id { get; init; }
        public ChannelLoadStatus Status { get; init; }
    }
}
