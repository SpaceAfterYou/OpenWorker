using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security.Cryptography;
using System.Text;

namespace ow.Framework.Database.Accounts
{
    public sealed class AccountMap : IEntityTypeConfiguration<AccountModel>
    {
        public void Configure(EntityTypeBuilder<AccountModel> builder)
        {
            using var sham = new SHA512Managed();
            var password = sham.ComputeHash(Encoding.UTF8.GetBytes("qwe123"));

            int id = 1;

            builder.HasData(
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "sawich",
                    Password = password
                },
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "Leeroy",
                    Password = password
                },
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "Tweekly",
                    Password = password
                },
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "Chelsea",
                    Password = password
                },
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "Dez",
                    Password = password
                },
                new AccountModel()
                {
                    Id = id++,
                    Nickname = "Godo",
                    Password = password
                }
            );
        }
    }
}