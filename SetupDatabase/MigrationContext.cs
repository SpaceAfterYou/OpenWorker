using ow.Framework.Database;
using Microsoft.EntityFrameworkCore;

namespace Migration.Contexts
{
    public sealed class MigrationContext : Context
    {
        public DbSet<ow.Framework.Database.Accounts.AccountModel> Account { get; set; }
        public DbSet<ow.Framework.Database.Characters.CharacterModel> Character { get; set; }
        public DbSet<ow.Framework.Database.Guilds.GuildModel> Guild { get; set; }
        public DbSet<ow.Framework.Database.Storages.ItemModel> Item { get; set; }
        public DbSet<ow.Framework.Database.AccouintPosts.AccountPostModel> AccountPost { get; set; }
        public DbSet<ow.Framework.Database.CharacterPosts.CharacterPostModel> CharacterPost { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new ow.Framework.Database.Accounts.AccountMap())
                .ApplyConfiguration(new ow.Framework.Database.Characters.CharacterMap())
                .ApplyConfiguration(new ow.Framework.Database.Guilds.GuildMap())
                .ApplyConfiguration(new ow.Framework.Database.Storages.ItemMap())
                .ApplyConfiguration(new ow.Framework.Database.AccouintPosts.AccountPostMap())
                .ApplyConfiguration(new ow.Framework.Database.CharacterPosts.CharacterPostMap());
        }
    }
}
