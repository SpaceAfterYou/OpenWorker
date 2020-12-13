using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Storage.Item
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