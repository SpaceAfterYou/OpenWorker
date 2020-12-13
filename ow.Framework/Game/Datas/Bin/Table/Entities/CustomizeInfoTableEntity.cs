using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = Hero;

    internal sealed class CustomizeInfoTableEntity : ICustomizeInfoTableEntity
    {
        public KeyType Id { get; }
        public byte Unknown1 { get; }
        public IReadOnlyList<byte> Unknown2 { get; }
        public IReadOnlyList<byte> Unknown3 { get; }
        public IReadOnlyList<string> Unknown4 { get; }

        internal CustomizeInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadHero();
            Unknown1 = br.ReadByte();
            Unknown2 = br.ReadByteArray(ItemsCount);
            Unknown3 = br.ReadByteArray(ItemsCount);
            Unknown4 = br.ReadByteLengthUnicodeStringArray(ItemsCount);
        }

        private const byte ItemsCount = 5;
    }
}