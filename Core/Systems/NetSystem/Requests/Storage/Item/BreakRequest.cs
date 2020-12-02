using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Storage.Item
{
    [Request]
    public readonly struct BreakRequest
    {
        public StorageType StorageType { get; }
        public ushort Slot { get; }

        public BreakRequest(BinaryReader br) =>
            (StorageType, Slot) = (br.ReadStorageType(), br.ReadUInt16());
    }
}
