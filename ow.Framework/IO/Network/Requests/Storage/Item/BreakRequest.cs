using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Storage.Item
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