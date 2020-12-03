using Core.DatabaseSystem;
using Microsoft.EntityFrameworkCore;

namespace Migration.Contexts
{
    public sealed class MigrationContext : Context
    {
        public DbSet<Core.DatabaseSystem.Accounts.AccountModel> Account { get; set; }
        public DbSet<Core.DatabaseSystem.Characters.CharacterModel> Character { get; set; }
        public DbSet<Core.DatabaseSystem.Guilds.GuildModel> Guild { get; set; }
        public DbSet<Core.DatabaseSystem.Storages.ItemModel> Item { get; set; }
        public DbSet<Core.DatabaseSystem.AccouintPosts.AccountPostModel> AccountPost { get; set; }
        public DbSet<Core.DatabaseSystem.CharacterPosts.CharacterPostModel> CharacterPost { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new Core.DatabaseSystem.Accounts.AccountMap())
                .ApplyConfiguration(new Core.DatabaseSystem.Characters.CharacterMap())
                .ApplyConfiguration(new Core.DatabaseSystem.Guilds.GuildMap())
                .ApplyConfiguration(new Core.DatabaseSystem.Storages.ItemMap())
                .ApplyConfiguration(new Core.DatabaseSystem.AccouintPosts.AccountPostMap())
                .ApplyConfiguration(new Core.DatabaseSystem.CharacterPosts.CharacterPostMap());
        }
    }
}