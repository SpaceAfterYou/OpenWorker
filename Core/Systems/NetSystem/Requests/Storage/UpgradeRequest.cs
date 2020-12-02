using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Storage
{
    [Request]
    public readonly struct UpgradeRequest
    {
        public StorageType StorageType { get; }

        public UpgradeRequest(BinaryReader br) =>
            StorageType = br.ReadStorageType();
    }
}
