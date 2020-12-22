using ow.Framework.Database.Characters;
using ow.Framework.Game.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Storages
{
    [Table("items")]
    public class ItemModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public uint PrototypeId { get; init; }

        [Required]
        public int CharacterId { get; init; }

        [Required]
        [ForeignKey(nameof(CharacterId))]
        public virtual CharacterModel Character { get; init; } = default!;

        [Required]
        public ushort Slot { get; init; }

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
        public UpgradeModel Upgrade { get; init; } = default!;

        [Required]
        public uint Color { get; init; }

        [Required]
        public uint DyeColor { get; init; }

        [Required]
        [Column(TypeName = "jsonb")]
        public StatModel[] Stats { get; init; } = default!;

        [Required]
        [Column(TypeName = "char(15)")]
        public string BroochSlots { get; init; } = default!;

        [Required]
        public uint TagId { get; init; } = 0;
    }
}