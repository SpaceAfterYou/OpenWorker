using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Systems.DatabaseSystem.Accounts
{
    [Table("accounts")]
    [Index("Nickname", IsUnique = true)]
    public class AccountModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        public int LastSelectedCharacterId { get; init; } = -1;

        [Required]
        [MaxLength(24)]
        public string Nickname { get; init; }

        [Required]
        public byte[] Password { get; init; }

        [Required]
        public ulong SoulCash { get; init; } = 0;
    }
}