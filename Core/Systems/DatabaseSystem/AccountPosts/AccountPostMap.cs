using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DatabaseSystem.AccouintPosts
{
    public sealed class AccountPostMap : IEntityTypeConfiguration<AccountPostModel>
    {
        public void Configure(EntityTypeBuilder<AccountPostModel> builder)
        {
        }
    }
}