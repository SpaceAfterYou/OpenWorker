using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Storage
{
    [Request]
    public readonly struct UpgradeRequest
    {
        public StorageType StorageType { get; }

        public UpgradeRequest(BinaryReader br) =>
            StorageType = br.ReadStorageType();
    }
}