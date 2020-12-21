using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    public sealed record ChattingCommandTableItem : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public byte PermissionLevel { get; }
        public IReadOnlyList<string> Command { get; }
        public uint ActionType { get; }

        internal ChattingCommandTableItem(BinaryReader br)
        {
            Id = br.ReadUInt16();
            PermissionLevel = br.ReadByte();
            Command = br.ReadByteLengthUnicodeStringArray(ItemsCount);
            ActionType = br.ReadUInt32();
        }

        private const byte ItemsCount = 5;
    }
}