using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.Accounts
{
    [Table("accounts")]
    [Index("Nickname", IsUnique = true)]
    public class AccountModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public int LastSelectedCharacter { get; init; } = -1;

        [Required]
        public int FavoriteCharacter { get; init; } = -1;

        [Required]
        public uint CharacterBackground { get; init; }

        [Required]
        [MaxLength(24)]
        public string Nickname { get; init; }

        [Required]
        public byte[] Password { get; init; }

        [Required]
        public ulong SoulCash { get; init; }
    }
}