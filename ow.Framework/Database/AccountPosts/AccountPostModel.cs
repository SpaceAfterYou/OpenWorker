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
        public int Id { get; init; }

        [Required]
        public int AccountId { get; init; }

        [ForeignKey(nameof(AccountId))]
        public Accounts.AccountModel Account { get; init; }
    }
}