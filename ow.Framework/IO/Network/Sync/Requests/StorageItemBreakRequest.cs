using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageItemBreakRequest
    {
        public StorageType StorageType { get; }
        public ushort Slot { get; }

        public StorageItemBreakRequest(BinaryReader br) => (StorageType, Slot) = (br.ReadStorageType(), br.ReadUInt16());
    }
}
