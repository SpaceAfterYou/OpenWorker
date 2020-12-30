using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.QuickSlots
{
    [Table("quick_slots")]
    [Index(nameof(CharacterId), IsUnique = true)]
    public class QuickSlotModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public virtual CharacterModel Character { get; set; } = default!;

        [Required]
        public int[] Acashic { get; set; } = default!;

        [Required]
        public int[] Cube { get; set; } = default!;

        [Required]
        public int[] Skill { get; set; } = default!;

        [Required]
        public int[] Gesture { get; set; } = default!;
    }
}