using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.IO.Network.Relay.Global;
using ow.Framework.Utils;
using ow.Service.Login.Game.Gate;
using ow.Service.Login.Game.Gate.Repository;
using SoulCore.IO.Network;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Login.Network
{
    [SyncServer(SyncServerType.Login)]
    public sealed class SyncServer : BaseServer<SyncServer, SyncSession>, IHostedService
    {
        public readonly Instance Instance;
        public readonly BinTable Tables;
        public readonly RGClient Relay;
        public readonly DistrictRepository Districts;
        public readonly IDbContextFactory<ItemContext> DatabaseItemFactory;
        public readonly IDbContextFactory<CharacterContext> DatabaseCharacterFactory;
        public readonly IDbContextFactory<AccountContext> DatabaseAccountFactory;

        public Task StartAsync(CancellationToken _)
        {
            Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken _)
        {
            Stop();
            return Task.CompletedTask;
        }

        public SyncServer(IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<CharacterContext> characterFactory, IDbContextFactory<AccountContext> accountFactory, IServiceProvider services, IConfiguration configuration) :
            base(services, GetIp(configuration), GetPort(configuration))
        {
            Instance = new(configuration);
            Tables = new(configuration);
            Relay = new(configuration);
            Districts = new(configuration);
            DatabaseItemFactory = itemFactory;
            DatabaseCharacterFactory = characterFactory;
            DatabaseAccountFactory = accountFactory;

            CommonUtils.PrintEnvironment(services.GetRequiredService<ILogger<SyncServer>>());
        }

        internal async ValueTask<AccountModel> GetAccountModelAsync(int id)
        {
            await using AccountContext context = DatabaseAccountFactory.CreateDbContext();
            return await context.Accounts.AsNoTracking().FirstAsync(c => c.Id == id).ConfigureAwait(true);
        }

        internal async ValueTask<CharacterList?> CreateCharacterListAsync(AccountModel accountModel, ushort gateId)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            await using CharacterContext characterContext = DatabaseCharacterFactory.CreateDbContext();
            await using ItemContext itemContext = DatabaseItemFactory.CreateDbContext();

            IQueryable<KeyValuePair<int, CharacterList.Character>> characters = characterContext.Characters.AsNoTracking()
                .Where(c => c.AccountId == accountModel.Id && c.Gate == gateId)
                .Select(model => KeyValuePair.Create(model.Id, new CharacterList.Character(model, itemContext)));

            stopwatch.Stop();

            return new CharacterList(accountModel, stopwatch, characters);
        }

        internal async ValueTask<CharacterList.Character> CreateEmptyCharacaterAsync(CharacterModel model)
        {
            await using ItemContext itemContext = DatabaseItemFactory.CreateDbContext();
            return new(model, itemContext);
        }

        private static string GetIp(IConfiguration configuration) => configuration
            .GetValue<string>($"World:Instance:{configuration["World"]}:Gate:Host:Ip");

        private static ushort GetPort(IConfiguration configuration) => configuration
            .GetValue<ushort>($"World:Instance:{configuration["World"]}:Gate:Host:Port");
    }
}