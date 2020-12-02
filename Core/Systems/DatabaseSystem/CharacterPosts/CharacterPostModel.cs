using SoulWorker.Types;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.DatabaseSystem.CharacterPosts
{
    [Table("character_post")]
    public sealed class CharacterPostModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        public uint CharacterId { get; init; }

        [ForeignKey("CharacterId")]
        public Characters.CharacterModel Character { get; init; }

        [Required]
        public CharacterPostType Type { get; init; }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateTime { get; init; }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModificationTime { get; init; }
    }
}