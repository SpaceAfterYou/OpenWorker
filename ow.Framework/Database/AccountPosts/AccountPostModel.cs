using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ow.Framework.Database.AccouintPosts
{
    [Table("account_posts")]
    public sealed class AccountPostModel
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; init; }

        [Required]
        public uint AccountId { get; init; }

        [ForeignKey("AccountId")]
        public Accounts.AccountModel Account { get; init; }
    }
}
