using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Systems.DatabaseSystem.Accounts
{
    [Table("accounts")]
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
        public byte[] Password { get; init; }

        [Required]
        [Column(TypeName = "CHAR(18)")]
        public string Mac { get; set; } = "00-00-00-00-00-00";

        [Required]
        public ulong SoulCash { get; init; } = 0;
    }
}