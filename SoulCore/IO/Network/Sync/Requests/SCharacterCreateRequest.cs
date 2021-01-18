using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SCharacterCreateRequest
    {
        public readonly struct HairInfo
        {
            public ushort Style { get; }
            public ushort Color { get; }

            public HairInfo(BinaryReader br) => (Style, Color) = (br.ReadUInt16(), br.ReadUInt16());
        }

        public readonly struct AppearanceInfo
        {
            private ushort Unknown1 { get; }
            private ushort Unknown2 { get; }
            public HairInfo Hair { get; }
            public ushort EyesColor { get; }
            public ushort SkinColor { get; }
            public HairInfo EquippedHair { get; }
            public ushort EquippedEyesColor { get; }
            public ushort EquippedSkinColor { get; }

            public AppearanceInfo(BinaryReader br)
            {
                Unknown1 = br.ReadUInt16();
                Unknown2 = br.ReadUInt16();
                Hair = new HairInfo(br);
                EyesColor = br.ReadUInt16();
                SkinColor = br.ReadUInt16();
                EquippedHair = new HairInfo(br);
                EquippedEyesColor = br.ReadUInt16();
                EquippedSkinColor = br.ReadUInt16();
            }
        };

        public readonly struct FashionInfo
        {
            private int Unknown1 { get; }
            private int Unknown2 { get; }
            public int Id { get; }
            public uint Color { get; }
            private int Unknown4 { get; }
            private int Unknown5 { get; }
            private int Unknown6 { get; }
            private uint Unknown7 { get; }

            public FashionInfo(BinaryReader br)
            {
                Unknown1 = br.ReadInt32();
                Unknown2 = br.ReadInt32();
                Id = br.ReadInt32();
                Color = br.ReadUInt32();
                Unknown4 = br.ReadInt32();
                Unknown5 = br.ReadInt32();
                Unknown6 = br.ReadInt32();
                Unknown7 = br.ReadUInt32();
            }
        }

        public readonly struct EquippedWeaponInfo
        {
            public int Id { get; }
            private byte Unknown1 { get; }
            private int Unknown2 { get; }

            public EquippedWeaponInfo(BinaryReader br)
            {
                Id = br.ReadInt32();
                Unknown1 = br.ReadByte();
                Unknown2 = br.ReadInt32();
            }
        }

        public readonly struct GuildInfo
        {
            public uint Id { get; }
            public string Name { get; }

            public GuildInfo(BinaryReader br)
            {
                Id = br.ReadUInt32();
                Name = br.ReadNumberLengthUtf8String();
            }
        }

        public readonly struct TitleInfo
        {
            public uint Primary { get; }
            public uint Secondary { get; }

            public TitleInfo(BinaryReader br) => (Primary, Secondary) = (br.ReadUInt32(), br.ReadUInt32());
        }

        public readonly struct EnergyInfo
        {
            public ushort Primary { get; }
            public ushort Extra { get; }

            public EnergyInfo(BinaryReader br) => (Primary, Extra) = (br.ReadUInt16(), br.ReadUInt16());
        }

        public readonly struct StatsInfo
        {
            public uint CurrentHP { get; }
            public uint MaxHP { get; }
            public uint CurrentSG { get; }
            public uint MaxSG { get; }
            private uint Unknown1 { get; }
            private uint Unknown2 { get; }
            public uint Stamina { get; }
            private uint Unknown3 { get; }
            private uint Unknown4 { get; }
            private uint Unknown5 { get; }
            public float MoveSpeed { get; }
            public float AttackSpeed { get; }

            public StatsInfo(BinaryReader br)
            {
                CurrentHP = br.ReadUInt32();
                MaxHP = br.ReadUInt32();
                CurrentSG = br.ReadUInt32();
                MaxSG = br.ReadUInt32();
                Unknown1 = br.ReadUInt32();
                Unknown2 = br.ReadUInt32();
                Unknown3 = br.ReadUInt32();
                Stamina = br.ReadUInt32();
                Unknown4 = br.ReadUInt32();
                Unknown5 = br.ReadUInt32();
                MoveSpeed = br.ReadSingle();
                AttackSpeed = br.ReadSingle();
            }
        }

        public readonly struct MainInfo
        {
            public int Id { get; }
            public string Name { get; }
            public Hero Hero { get; }
            public CharacterAdvancement Advancement { get; }
            public uint PhotoId { get; }
            public AppearanceInfo Appearance { get; }
            public byte Level { get; }
            private byte Unknown1 { get; }
            private IReadOnlyList<byte> Unknown2 { get; }
            public EquippedWeaponInfo EquippedWeapon { get; }

            public MainInfo(BinaryReader br)
            {
                Id = br.ReadInt32();
                Name = br.ReadNumberLengthUnicodeString();
                Hero = br.ReadHero();
                Advancement = br.ReadCharacterAdvancement();
                PhotoId = br.ReadUInt32();
                Appearance = new AppearanceInfo(br);
                Level = br.ReadByte();
                Unknown1 = br.ReadByte();
                Unknown2 = new List<byte>(br.ReadBytes(10));
                EquippedWeapon = new EquippedWeaponInfo(br);
            }
        }

        public readonly struct CosmeticInfo
        {
            public IReadOnlyList<FashionInfo> Fashion { get; }

            public CosmeticInfo(BinaryReader br) => Fashion = Enumerable.Repeat(0, Defines.FashionRows).Select(_ => new FashionInfo(br)).ToArray();
        }

        public readonly struct MetaInfo
        {
            private uint Unknown3 { get; }
            public TitleInfo Title { get; }
            public GuildInfo Guild { get; }
            private uint Unknown4 { get; }
            public StatsInfo Stats { get; }
            private ushort Unknown5 { get; }
            private byte Unknown6 { get; }
            private ushort Unknown7 { get; }
            private ushort Unknown8 { get; }
            public EnergyInfo Energy { get; }
            private IReadOnlyList<byte> Unknown9 { get; }

            public MetaInfo(BinaryReader br)
            {
                Unknown3 = br.ReadUInt32();
                Title = new TitleInfo(br);
                Guild = new GuildInfo(br);
                Unknown4 = br.ReadUInt32();
                Stats = new StatsInfo(br);
                Unknown5 = br.ReadUInt16();
                Unknown6 = br.ReadByte();
                Unknown7 = br.ReadUInt16();
                Unknown8 = br.ReadUInt16();
                Energy = new EnergyInfo(br);
                Unknown9 = br.ReadBytes(13);
            }
        }

        public readonly struct CharacterInfo
        {
            public MainInfo Main { get; }
            public CosmeticInfo Cosmetic { get; }
            public MetaInfo Meta { get; }

            public CharacterInfo(BinaryReader br)
            {
                Main = new MainInfo(br);
                Cosmetic = new CosmeticInfo(br);
                Meta = new MetaInfo(br);
            }
        }

        public CharacterInfo Character { get; }
        public byte Slot { get; }
        private uint Unknown1 { get; }
        private uint Unknown2 { get; }
        public byte Unknown3 { get; }
        public uint Outfit { get; }

        public SCharacterCreateRequest(BinaryReader br)
        {
            Character = new CharacterInfo(br);
            Slot = br.ReadByte();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadByte();
            Outfit = br.ReadUInt32();
        }
    }
}
