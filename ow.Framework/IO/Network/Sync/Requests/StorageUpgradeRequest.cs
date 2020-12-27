using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageUpgradeRequest
    {
        public StorageType Storage { get; }

        public StorageUpgradeRequest(BinaryReader br) => Storage = br.ReadStorageType();
    }
}
