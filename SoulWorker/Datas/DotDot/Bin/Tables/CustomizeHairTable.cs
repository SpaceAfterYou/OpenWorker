using Core.Systems.NetSystem.Extensions;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace SoulWorker.Datas.DotDot.Bin.Tables
{
    using KeyType = HeroType;

    public interface ICustomizeHairTable : IReadOnlyList<CustomizeHairTable.Entry>
    {
        public CustomizeHairTable.Entry this[KeyType index] => this[(int)index];
    }

    public static class CustomizeHairTable
    {
        public sealed class Entry : ITableItemEntry<KeyType>
        {
            public KeyType Id { get; }
            public uint Unknown5 { get; }
            public uint Unknown6 { get; }
            public uint Unknown7 { get; }
            public uint Unknown8 { get; }
            public uint Unknown9 { get; }
            public uint Unknown10 { get; }
            public uint Unknown11 { get; }
            public uint Unknown12 { get; }
            public uint Unknown13 { get; }
            public uint Unknown14 { get; }
            public IReadOnlyList<uint> Style { get; }
            public uint Unknown25 { get; }
            public uint Unknown26 { get; }
            public uint Unknown27 { get; }
            public uint Unknown28 { get; }
            public uint Unknown29 { get; }
            public uint Unknown30 { get; }
            public uint Unknown31 { get; }
            public uint Unknown32 { get; }
            public uint Unknown33 { get; }
            public uint Unknown34 { get; }
            public IReadOnlyList<string> Icons { get; }
            public IReadOnlyList<uint> Color { get; }

            public static ICustomizeHairTable Read(VArchive archive) =>
                TableReader<KeyType, Entry>.Read(archive, "tb_Customize_Hair") as ICustomizeHairTable;

            internal Entry(BinaryReader br)
            {
                Id = br.ReadHeroType();

                Unknown5 = br.ReadUInt32();
                Unknown6 = br.ReadUInt32();
                Unknown7 = br.ReadUInt32();
                Unknown8 = br.ReadUInt32();
                Unknown9 = br.ReadUInt32();

                Unknown10 = br.ReadUInt32();
                Unknown11 = br.ReadUInt32();
                Unknown12 = br.ReadUInt32();
                Unknown13 = br.ReadUInt32();
                Unknown14 = br.ReadUInt32();

                Style = Enumerable.Repeat(0, 10).Select((e) => br.ReadUInt32()).ToArray();

                Unknown25 = br.ReadUInt32();
                Unknown26 = br.ReadUInt32();
                Unknown27 = br.ReadUInt32();
                Unknown28 = br.ReadUInt32();
                Unknown29 = br.ReadUInt32();

                Unknown30 = br.ReadUInt32();
                Unknown31 = br.ReadUInt32();
                Unknown32 = br.ReadUInt32();
                Unknown33 = br.ReadUInt32();
                Unknown34 = br.ReadUInt32();

                Icons = Enumerable.Repeat(0, 10).Select((e) => br.ReadByteLengthUnicodeString()).ToArray();
                Color = Enumerable.Repeat(0, 10).Select((e) => br.ReadUInt32()).ToArray();
            }
        }
    }
}