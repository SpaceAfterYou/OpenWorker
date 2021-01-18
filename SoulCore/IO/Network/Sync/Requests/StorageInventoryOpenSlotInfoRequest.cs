using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageInventoryOpenSlotInfoRequest
    {
        public IReadOnlyList<StorageType> Values { get; }

        public StorageInventoryOpenSlotInfoRequest(BinaryReader br) => Values = Enumerable.Repeat(0, br.ReadByte()).Select(_ => br.ReadStorageType()).ToArray();
    }
}
