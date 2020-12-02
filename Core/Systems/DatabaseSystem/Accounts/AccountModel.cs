using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.DatabaseSystem.Accounts
{
    [Table("account")]
    [Index("SessionKey")]
    [Index("Nickname", IsUnique = true)]
    public class AccountModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        public int LastSelectedCharacter { get; init; } = -1;

        [Required]
        public ulong SessionKey { get; set; } = 0;

        [Required]
        [MaxLength(24)]
        public string Nickname { get; init; }

        [Required]
        [Column(TypeName = "CHAR(64)")]
        public byte[] Password { get; init; }

        [Required]
        [Column(TypeName = "CHAR(18)")]
        public string Mac { get; set; } = "00-00-00-00-00-00";

        [Required]
        public ulong SoulCash { get; init; } = 0;

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateTime { get; init; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModificationTime { get; init; }
    }
}