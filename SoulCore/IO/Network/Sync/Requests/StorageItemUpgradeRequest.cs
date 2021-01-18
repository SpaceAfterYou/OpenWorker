using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageItemUpgradeRequest
    {
        public StorageType StorageType { get; }
        public ushort Slot { get; }

        public StorageItemUpgradeRequest(BinaryReader br) => (StorageType, Slot) = (br.ReadStorageType(), br.ReadUInt16());
    }
}
