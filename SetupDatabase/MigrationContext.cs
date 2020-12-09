using Microsoft.EntityFrameworkCore;
using ow.Framework.Database;
using ow.Framework.Database.AccouintPosts;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.CharacterPosts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Guilds;
using ow.Framework.Database.Storages;

namespace SetupDatabase
{
    public sealed class MigrationContext : BaseDbContext
    {
        public DbSet<AccountModel> Account { get; set; }
        public DbSet<CharacterModel> Character { get; set; }
        public DbSet<GuildModel> Guild { get; set; }
        public DbSet<ItemModel> Item { get; set; }
        public DbSet<AccountPostModel> AccountPost { get; set; }
        public DbSet<CharacterPostModel> CharacterPost { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder
            .ApplyConfiguration(new AccountMap())
            .ApplyConfiguration(new CharacterMap())
            .ApplyConfiguration(new GuildMap())
            .ApplyConfiguration(new ItemMap())
            .ApplyConfiguration(new AccountPostMap())
            .ApplyConfiguration(new CharacterPostMap());
    }
}