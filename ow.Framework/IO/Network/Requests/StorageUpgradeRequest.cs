using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct StorageUpgradeRequest
    {
        public StorageType Storage { get; }

        public StorageUpgradeRequest(BinaryReader br) => Storage = br.ReadStorageType();
    }
}
