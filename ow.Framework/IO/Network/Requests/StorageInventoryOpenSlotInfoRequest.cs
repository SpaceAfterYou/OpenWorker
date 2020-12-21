using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct StorageInventoryOpenSlotInfoRequest
    {
        public IReadOnlyList<StorageType> Values { get; }

        public StorageInventoryOpenSlotInfoRequest(BinaryReader br) => Values = Enumerable.Repeat(0, br.ReadByte()).Select(_ => br.ReadStorageType()).ToArray();
    }
}
