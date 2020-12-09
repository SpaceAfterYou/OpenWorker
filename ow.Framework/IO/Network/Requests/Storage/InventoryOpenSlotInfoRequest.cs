using ow.Framework.Extensions;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Requests.Storage
{
    [Request]
    public readonly struct InventoryOpenSlotInfoRequest
    {
        public IReadOnlyList<StorageType> Storages { get; }

        public InventoryOpenSlotInfoRequest(BinaryReader br) =>
            Storages = Enumerable.Repeat(0, br.ReadByte()).Select(_ => br.ReadStorageType()).ToArray();
    }
}