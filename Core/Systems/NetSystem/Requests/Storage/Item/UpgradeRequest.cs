using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Storage.Item
{
    [Request]
    public readonly struct UpgradeRequest
    {
        public StorageType StorageType { get; }
        public ushort SlotId { get; }

        public UpgradeRequest(BinaryReader br) =>
            (StorageType, SlotId) = (br.ReadStorageType(), br.ReadUInt16());
    }
}
