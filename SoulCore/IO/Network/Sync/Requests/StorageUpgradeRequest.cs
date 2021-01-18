using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageUpgradeRequest
    {
        public StorageType Storage { get; }

        public StorageUpgradeRequest(BinaryReader br) => Storage = br.ReadStorageType();
    }
}
