using SoulWorker.Types;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.DatabaseSystem.Storages
{
    public sealed class StatModel
    {
        public ItemStatType Id { get; init; }
        public int Value { get; init; }
    }

    public sealed class UpgradeModel
    {
        public byte UsedAttempts { get; init; }
        public byte CurrentLevel { get; init; }
    }

    [Table("storage")]
    public class StorageModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        public uint PrototypeId { get; init; }

        [Required]
        public uint CharacterId { get; init; }

        [Required]
        [ForeignKey(nameof(CharacterId))]
        public Characters.CharacterModel Character { get; init; }

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

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateTime { get; init; }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModificationTime { get; init; }
    }
}