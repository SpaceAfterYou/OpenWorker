using ow.Framework.Game.Types;
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
        public uint Id { get; init; }

        [Required]
        public uint CharacterId { get; init; }

        [ForeignKey("CharacterId")]
        public Characters.CharacterModel Character { get; init; }

        [Required]
        public CharacterPostType Type { get; init; }
    }
}
