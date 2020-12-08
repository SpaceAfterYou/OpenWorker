using Core.Systems.NetSystem.Extensions;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Systems.GameSystem.Datas.Bin.Table.Entities
{
    using KeyType = HeroType;

    public sealed class CustomizeEyesTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public IReadOnlyList<uint> Unknown1 { get; }
        public IReadOnlyList<string> Icons { get; }
        public IReadOnlyList<uint> Color { get; }

        internal CustomizeEyesTableEntity(BinaryReader br)
        {
            Id = br.ReadHeroType();
            Unknown1 = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadUInt32()).ToArray();
            Icons = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadByteLengthUnicodeString()).ToArray();
            Color = Enumerable.Repeat(0, ItemsCount).Select((e) => br.ReadUInt32()).ToArray();
        }

        private const byte ItemsCount = 10;
    }
}
