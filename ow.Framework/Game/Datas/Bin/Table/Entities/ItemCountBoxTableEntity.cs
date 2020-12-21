using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    public sealed record ItemCountBoxTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public byte UsageCount { get; }
        public IReadOnlyList<ushort> UsageGroup { get; }

        internal ItemCountBoxTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            UsageCount = br.ReadByte();
            UsageGroup = br.ReadUInt16Array(ItemsCount);
        }

        private const byte ItemsCount = 30;
    }
}