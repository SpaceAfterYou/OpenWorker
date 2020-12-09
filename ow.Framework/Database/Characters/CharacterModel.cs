using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Ids;
using ow.Framework.Game.Types;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Characters
{
    public sealed class ProfileModel
    {
        public ProfileStatusType Status { get; set; } = ProfileStatusType.Rookie;
        public string About { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public sealed class StorageModel
    {
        public StorageType Type { get; set; }
        public byte Upgrades { get; set; }
    }

    public sealed class HairModel
    {
        public ushort Style { get; set; }

        public ushort Color { get; set; }
    }

    public sealed class ApperanceModel
    {
        public HairModel Hair { get; set; }
        public ushort EyeColor { get; set; }
        public ushort SkinColor { get; set; }
        public HairModel EquippedHair { get; set; }
        public ushort EquippedEyeColor { get; set; }
        public ushort EquippedSkinColor { get; set; }
    }

    public sealed class Vector3Model
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }
    }

    public sealed class PlaceModel
    {
        public Vector3Model Position { get; set; }
        public float Rotation { get; set; }
        public ushort Location { get; set; }
    }

    public sealed class EnergyModel
    {
        public ushort Primary { get; set; } = 200;

        public ushort Extra { get; set; } = 0;
    }

    public sealed class TitleModel
    {
        public uint Primary { get; set; } = 0;

        public uint Secondary { get; set; } = 0;
    }

    public sealed class InventoryModel
    {
        public ulong Ether { get; set; } = 0;

        public ulong BattlePoints { get; set; } = 0;

        public ulong Zenny { get; set; } = 0;
    }

    public sealed class BankModel
    {
        public ulong Zenny { get; set; } = 0;
    }

    [Table("characters")]
    [Index("Name", IsUnique = true)]
    public sealed class CharacterModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public AccountModel Account { get; set; }

        [Required]
        public ushort GateId { get; set; }

        [Required]
        public byte SlotId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public HeroId Hero { get; set; }

        [Required]
        public byte Level { get; set; } = 1;

        [Required]
        public ulong Exp { get; set; } = 0;

        [Required]
        [Column(TypeName = "jsonb")]
        public InventoryModel Inventory { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public BankModel Bank { get; set; }

        [Required]
        public uint[] GeturesIds { get; set; }

        [Required]
        public uint PortraitId { get; set; } = 0;

        [Required]
        public byte Advancement { get; set; } = 0;

        [Required]
        [Column(TypeName = "jsonb")]
        public ApperanceModel Appearance { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public PlaceModel Place { get; set; }

        [Required]
        public uint[] LearnedSkill { get; set; }

        [Required]
        public uint[] QuickSlot { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public EnergyModel Energy { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public TitleModel Title { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public StorageModel[] Storage { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public ProfileModel Profile { get; set; }
    }
}