using ow.Framework.Game.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.CharacterPosts
{
    [Table("character_posts")]
    public sealed class CharacterPostModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public int CharacterId { get; init; }

        [ForeignKey(nameof(CharacterId))]
        public Characters.CharacterModel Character { get; init; }

        [Required]
        public CharacterPostType Type { get; init; }
    }
}