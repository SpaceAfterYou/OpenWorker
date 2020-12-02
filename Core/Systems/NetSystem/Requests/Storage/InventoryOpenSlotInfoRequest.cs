using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Systems.NetSystem.Requests.Storage
{
    [Request]
    public readonly struct InventoryOpenSlotInfoRequest
    {
        public IReadOnlyList<StorageType> Storages { get; }

        public InventoryOpenSlotInfoRequest(BinaryReader br)
        {
            Storages = Enumerable
                .Range(0, br.ReadByte())
                .Select(_ => br.ReadStorageType())
                .ToArray();
        }
    }
}
