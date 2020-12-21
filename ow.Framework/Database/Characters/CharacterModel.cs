using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Characters
{
    [Table("characters")]
    [Index("Name", IsUnique = true)]
    public class CharacterModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual AccountModel Account { get; set; } = default!;

        [Required]
        public ushort Gate { get; set; }

        [Required]
        public byte Slot { get; set; }

        [Required]
        [MaxLength(Defines.MaxCharacterNameLength)]
        public string Name { get; set; } = default!;

        [Required]
        public Hero Hero { get; set; }

        [Required]
        public byte Level { get; set; } = 1;

        [Required]
        public ulong Exp { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public InventoryModel Inventory { get; set; } = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public BankModel Bank { get; set; } = default!;

        [Required]
        public uint[] Gestures { get; set; } = default!;

        [Required]
        public uint Photo { get; set; }

        [Required]
        public CharacterAdvancement Advancement { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public AppearanceModel Appearance { get; set; } = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public PlaceModel Place { get; set; } = default!;

        [Required]
        public uint[] LearnedSkill { get; set; } = default!;

        [Required]
        public uint[] QuickSlot { get; set; }
         = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public EnergyModel Energy { get; set; } = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public TitleModel Title { get; set; } = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public StorageModel[] Storage { get; set; } = default!;

        [Required]
        [Column(TypeName = "jsonb")]
        public ProfileModel Profile { get; set; } = default!;
    }
}