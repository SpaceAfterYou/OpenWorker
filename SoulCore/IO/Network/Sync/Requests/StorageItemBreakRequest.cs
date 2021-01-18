﻿using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageItemBreakRequest
    {
        public StorageType StorageType { get; }
        public ushort Slot { get; }

        public StorageItemBreakRequest(BinaryReader br) => (StorageType, Slot) = (br.ReadStorageType(), br.ReadUInt16());
    }
}
