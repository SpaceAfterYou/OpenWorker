using ow.Framework.Extensions;
using ow.Framework.Game.Ids;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = HeroId;

    public sealed class CustomizeInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public byte Unknown1 { get; }
        public IReadOnlyList<byte> Unknown2 { get; }
        public IReadOnlyList<byte> Unknown3 { get; }
        public IReadOnlyList<string> Unknown4 { get; }

        internal CustomizeInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadHeroId();
            Unknown1 = br.ReadByte();
            Unknown2 = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadByte()).ToArray();
            Unknown3 = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadByte()).ToArray();
            Unknown4 = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadByteLengthUnicodeString()).ToArray();
        }

        private const byte ItemsCount = 5;
    }
}