using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Guilds
{
    [Table("guilds")]
    public sealed class GuildModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        [MaxLength(12)]
        public string Name { get; init; } = default!;

        [Required]
        [MaxLength(1024)]
        public string Description { get; init; } = default!;
    }
}