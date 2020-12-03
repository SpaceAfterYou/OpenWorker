using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.DatabaseSystem.Guilds
{
    [Table("guilds")]
    public sealed class GuildModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        [MaxLength(12)]
        public string Name { get; init; }

        [Required]
        [MaxLength(1024)]
        public string Description { get; init; }
    }
}