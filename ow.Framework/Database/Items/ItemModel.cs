using ow.Framework.Database.Characters;
using ow.Framework.Game.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Storages
{
    public sealed class StatModel
    {
        public ItemStatId Id { get; init; }
        public int Value { get; init; }
    }

    public sealed class UpgradeModel
    {
        public byte UsedAttempts { get; init; }
        public byte CurrentLevel { get; init; }
    }

    [Table("items")]
    public class ItemModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public int PrototypeId { get; init; }

        [Required]
        public int CharacterId { get; init; }

        [Required]
        [ForeignKey(nameof(CharacterId))]
        public virtual CharacterModel Character { get; init; }

        [Required]
        public ushort SlotId { get; init; }

        [Required]
        public StorageType StorageType { get; init; }

        [Required]
        public ushort Count { get; init; }

        [Required]
        public byte Durability { get; init; } = 100;

        [Required]
        public byte Slots { get; init; }

        [Required]
        public byte Quantly { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public UpgradeModel Upgrade { get; init; }

        [Required]
        public uint Color { get; init; }

        [Required]
        public uint DyeColor { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public StatModel[] Stats { get; init; }

        [Required]
        [Column(TypeName = "char(15)")]
        public string BroochSlots { get; init; }

        [Required]
        public uint TagId { get; init; } = 0;
    }
}