using Core.Systems.DatabaseSystem;
using Microsoft.EntityFrameworkCore;

namespace Migration.Contexts
{
    public sealed class MigrationContext : Context
    {
        public DbSet<Core.Systems.DatabaseSystem.Accounts.AccountModel> Account { get; set; }
        public DbSet<Core.Systems.DatabaseSystem.Characters.CharacterModel> Character { get; set; }
        public DbSet<Core.Systems.DatabaseSystem.Guilds.GuildModel> Guild { get; set; }
        public DbSet<Core.Systems.DatabaseSystem.Storages.ItemModel> Item { get; set; }
        public DbSet<Core.Systems.DatabaseSystem.AccouintPosts.AccountPostModel> AccountPost { get; set; }
        public DbSet<Core.Systems.DatabaseSystem.CharacterPosts.CharacterPostModel> CharacterPost { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.Accounts.AccountMap())
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.Characters.CharacterMap())
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.Guilds.GuildMap())
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.Storages.ItemMap())
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.AccouintPosts.AccountPostMap())
                .ApplyConfiguration(new Core.Systems.DatabaseSystem.CharacterPosts.CharacterPostMap());
        }
    }
}