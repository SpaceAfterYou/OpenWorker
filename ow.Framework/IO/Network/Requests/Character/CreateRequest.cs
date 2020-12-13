using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct CreateRequest
    {
        public CreateCharacter Character { get; }
        public byte SlotId { get; }
        private uint Unknown1 { get; }
        private uint Unknown2 { get; }
        public byte Unknown3 { get; }
        public uint OutfitId { get; }

        public CreateRequest(BinaryReader br)
        {
            Character = new CreateCharacter(br);
            SlotId = br.ReadByte();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadByte();
            OutfitId = br.ReadUInt32();
        }
    }

    public readonly struct CreateCharacterHair
    {
        public ushort Style { get; }
        public ushort Color { get; }

        public CreateCharacterHair(BinaryReader br)
            => (Style, Color) = (br.ReadUInt16(), br.ReadUInt16());
    }

    public readonly struct CreateCharacterAppearance
    {
        private ushort Unknown1 { get; }
        private ushort Unknown2 { get; }
        public CreateCharacterHair Hair { get; }
        public ushort EyesColor { get; }
        public ushort SkinColor { get; }
        public CreateCharacterHair EquippedHair { get; }
        public ushort EquippedEyesColor { get; }
        public ushort EquippedSkinColor { get; }

        public CreateCharacterAppearance(BinaryReader br)
        {
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            Hair = new CreateCharacterHair(br);
            EyesColor = br.ReadUInt16();
            SkinColor = br.ReadUInt16();
            EquippedHair = new CreateCharacterHair(br);
            EquippedEyesColor = br.ReadUInt16();
            EquippedSkinColor = br.ReadUInt16();
        }
    };

    public readonly struct CreateCharacterFashion
    {
        private int Unknown1 { get; }
        private int Unknown2 { get; }
        public int Id { get; }
        public uint Color { get; }
        private int Unknown4 { get; }
        private int Unknown5 { get; }
        private int Unknown6 { get; }
        private uint Unknown7 { get; }

        public CreateCharacterFashion(BinaryReader br)
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

    public readonly struct CreateCharacterEquippedWeapon
    {
        public int Id { get; }
        private byte Unknown1 { get; }
        private int Unknown2 { get; }

        public CreateCharacterEquippedWeapon(BinaryReader br)
        {
            Id = br.ReadInt32();
            Unknown1 = br.ReadByte();
            Unknown2 = br.ReadInt32();
        }
    }

    public readonly struct CreateCharacterGuild
    {
        public uint Id { get; }
        public string Name { get; }

        public CreateCharacterGuild(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Name = br.ReadNumberLengthUtf8String();
        }
    }

    public readonly struct CreateCharacterTitle
    {
        public uint Primary { get; }
        public uint Secondary { get; }

        public CreateCharacterTitle(BinaryReader br)
        {
            Primary = br.ReadUInt32();
            Secondary = br.ReadUInt32();
        }
    }

    public readonly struct CreateCharacterEnergy
    {
        public ushort Primary { get; }
        public ushort Extra { get; }

        public CreateCharacterEnergy(BinaryReader br)
        {
            Primary = br.ReadUInt16();
            Extra = br.ReadUInt16();
        }
    }

    public readonly struct CreateCharacterStats
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

        public CreateCharacterStats(BinaryReader br)
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

    public readonly struct CreateCharacterMain
    {
        public int Id { get; }
        public string Name { get; }
        public Hero Hero { get; }
        public byte Advancement { get; }
        public uint Portrait { get; }
        public CreateCharacterAppearance Appearance { get; }
        public byte Level { get; }
        private byte Unknown1 { get; }
        private IReadOnlyList<byte> Unknown2 { get; }
        public CreateCharacterEquippedWeapon EquippedWeapon { get; }

        public CreateCharacterMain(BinaryReader br)
        {
            Id = br.ReadInt32();
            Name = br.ReadNumberLengthUnicodeString();
            Hero = br.ReadHero();
            Advancement = br.ReadByte();
            Portrait = br.ReadUInt32();
            Appearance = new CreateCharacterAppearance(br);
            Level = br.ReadByte();
            Unknown1 = br.ReadByte();
            Unknown2 = new List<byte>(br.ReadBytes(10));
            EquippedWeapon = new CreateCharacterEquippedWeapon(br);
        }
    }

    public readonly struct CreateCharacterCosmetic
    {
        public IReadOnlyList<CreateCharacterFashion> Fashion { get; }

        public CreateCharacterCosmetic(BinaryReader br) =>
            Fashion = Enumerable.Repeat(0, Defines.FashionRows).Select(_ => new CreateCharacterFashion(br)).ToArray();
    }

    public readonly struct CreateCharacterMeta
    {
        private uint Unknown3 { get; }
        public CreateCharacterTitle Title { get; }
        public CreateCharacterGuild Guild { get; }
        private uint Unknown4 { get; }
        public CreateCharacterStats Stats { get; }
        private ushort Unknown5 { get; }
        private byte Unknown6 { get; }
        private ushort Unknown7 { get; }
        private ushort Unknown8 { get; }
        public CreateCharacterEnergy Energy { get; }
        private IReadOnlyList<byte> Unknown9 { get; }

        public CreateCharacterMeta(BinaryReader br)
        {
            Unknown3 = br.ReadUInt32();
            Title = new CreateCharacterTitle(br);
            Guild = new CreateCharacterGuild(br);
            Unknown4 = br.ReadUInt32();
            Stats = new CreateCharacterStats(br);
            Unknown5 = br.ReadUInt16();
            Unknown6 = br.ReadByte();
            Unknown7 = br.ReadUInt16();
            Unknown8 = br.ReadUInt16();
            Energy = new CreateCharacterEnergy(br);
            Unknown9 = br.ReadBytes(13);
        }
    }

    public readonly struct CreateCharacter
    {
        public CreateCharacterMain Main { get; }
        public CreateCharacterCosmetic Cosmetic { get; }
        public CreateCharacterMeta Meta { get; }

        public CreateCharacter(BinaryReader br)
        {
            Main = new CreateCharacterMain(br);
            Cosmetic = new CreateCharacterCosmetic(br);
            Meta = new CreateCharacterMeta(br);
        }
    }
}