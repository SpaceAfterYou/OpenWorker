using Microsoft.EntityFrameworkCore;
using ow.Framework.Game.Ids;
using ow.Framework.Game.Types;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Characters
{
    public sealed class ProfileModel
    {
        public ProfileStatusType Status { get; init; }
        public string About { get; init; }
        public string Note { get; init; }
    }

    public sealed class StorageModel
    {
        public StorageType Type { get; init; }
        public byte Upgrades { get; init; }
    }

    public sealed class HairModel
    {
        public ushort Style { get; init; }

        public ushort Color { get; init; }
    }

    public sealed class ApperanceModel
    {
        public HairModel Hair { get; init; }
        public ushort EyeColor { get; init; }
        public ushort SkinColor { get; init; }
        public HairModel EquippedHair { get; init; }
        public ushort EquippedEyeColor { get; init; }
        public ushort EquippedSkinColor { get; init; }
    }

    public sealed class Vector3Model
    {
        public float X { get; init; }

        public float Y { get; init; }

        public float Z { get; init; }
    }

    public sealed class PlaceModel
    {
        public Vector3Model Position { get; init; }
        public float Rotation { get; init; }
        public ushort Location { get; init; }
    }

    public sealed class EnergyModel
    {
        public ushort Primary { get; init; } = 200;

        public ushort Extra { get; init; } = 0;
    }

    public sealed class TitleModel
    {
        public uint Primary { get; init; } = 0;

        public uint Secondary { get; init; } = 0;
    }

    public sealed class InventoryModel
    {
        public ulong Ether { get; init; } = 0;

        public ulong BattlePoints { get; init; } = 0;

        public ulong Zenny { get; init; } = 0;
    }

    public sealed class BankModel
    {
        public ulong Zenny { get; init; } = 0;
    }

    [Table("characters")]
    [Index("Name", IsUnique = true)]
    public sealed class CharacterModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(TypeName = "SERIAL")]
        public uint Id { get; init; }

        [Required]
        public uint AccountId { get; init; }

        [ForeignKey("AccountId")]
        public Accounts.AccountModel Account { get; init; }

        [Required]
        public ushort GateId { get; init; }

        [Required]
        public byte SlotId { get; init; }

        [Required]
        public string Name { get; init; }

        [Required]
        public HeroId Hero { get; init; }

        [Required]
        public byte Level { get; init; } = 1;

        [Required]
        public ulong Exp { get; init; } = 0;

        [Required]
        [Column(TypeName = "jsonb")]
        public InventoryModel Inventory { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public BankModel Bank { get; init; }

        [Required]
        public uint PortraitId { get; init; } = 0;

        [Required]
        public byte Advancement { get; init; } = 0;

        [Required]
        public uint[] Gesture { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public ApperanceModel Appearance { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public PlaceModel Place { get; init; }

        [Required]
        public uint[] LearnedSkill { get; init; }

        [Required]
        public uint[] QuickSlot { get; init; }

        [Required]
        public uint[] SkillSlot { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public EnergyModel Energy { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public TitleModel Title { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public StorageModel[] Storage { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public ProfileModel Profile { get; init; }
    }
}