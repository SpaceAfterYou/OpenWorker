using DefaultEcs;
using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Characters : List<Entity>, IDisposable
    {
        public TimeSpan InitializeTime { get; init; }
        public Entity LastSelected { get; set; }
        public Entity Favorite { get; set; }

        private readonly World _entities = new();

        public void Dispose() => _entities.Dispose();

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables, World entities) : base(GetCharacterSlots(entities))
        {
            _entities = entities;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId))
            {
                this[model.Slot].Set<EntityCharacter>(new(model, tables));
                this[model.Slot].Set<IStatsEntity>(new StatsEntity());
            }

            if (accountModel.LastSelectedCharacter != -1)
                LastSelected = Find(c => c.Get<EntityCharacter>().Id == accountModel.LastSelectedCharacter);

            if (accountModel.FavoriteCharacter != -1)
                Favorite = Find(c => c.Get<EntityCharacter>().Id == accountModel.FavoriteCharacter);

            stopwatch.Stop();
            InitializeTime = stopwatch.Elapsed;
        }

        private static IEnumerable<CharacterModel> GetCharacterModels(AccountModel accountModel, ushort gateId)
        {
            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                yield return model;
        }

        private static Entity[] GetCharacterSlots(World _entities) =>
            Enumerable.Repeat(_entities.CreateEntity(), Defines.CharactersCount).ToArray();
    }
}