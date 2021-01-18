using SoulCore.Database.Characters;
using SoulCore.Game.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoulCore.Database.CharacterPosts
{
    [Table("character_posts")]
    public class CharacterPostModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public int CharacterId { get; init; }

        [ForeignKey(nameof(CharacterId))]
        public virtual CharacterModel Character { get; init; } = default!;

        [Required]
        public CharacterPostType Type { get; init; }
    }
}
