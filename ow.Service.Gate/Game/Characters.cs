using DefaultEcs;
using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using ow.Framework.Game.Storage;
using ow.Framework.Utils;
using ow.Service.Gate.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Characters : Dictionary<int, Entity>, IDisposable
    {
        public TimeSpan InitializeTime { get; }
        public Entity? Favorite { get; set; }
        public Entity? LastSelected { get; set; }

        private readonly World _entities = new();

        public void Delete(int id)
        {
            if (!Remove(id, out Entity entity))
                NetworkUtils.DropSession();

            try
            {
                if (LastSelected?.Get<EntityCharacter>().Id == id)
                    LastSelected = Count > 0 ? this.First().Value : null;

                if (id == Favorite?.Get<EntityCharacter>().Id)
                    Favorite = null;
            }
            finally
            {
                entity.Dispose();
            }
        }

        public void Create(CharacterModel model, BinTables tables)
        {
            Push(new(model, tables), new(model.Place, tables), new(), new());

            LastSelected = this[model.Id];
        }

        public void Dispose() => _entities.Dispose();

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables, World entities)
        {
            _entities = entities;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId))
                Push(new(model, tables), new(model.Place, tables), new(), new(model));

            if (accountModel.LastSelectedCharacter != -1 && TryGetValue(accountModel.LastSelectedCharacter, out Entity last))
                LastSelected = last;

            if (accountModel.FavoriteCharacter != -1 && TryGetValue(accountModel.FavoriteCharacter, out Entity favorite))
                Favorite = favorite;

            stopwatch.Stop();
            InitializeTime = stopwatch.Elapsed;
        }

        private void Push(EntityCharacter character, PlaceEntity place, GateStatsEntity stats, GateStorageEntity storage)
        {
            Entity entity = _entities.CreateEntity();

            entity.Set(character);
            entity.Set(place);
            entity.Set<IStatsEntity>(stats);
            entity.Set<IStorageEntity>(storage);

            if (!TryAdd(character.Id, entity))
                NetworkUtils.DropSession();
        }

        private static IEnumerable<CharacterModel> GetCharacterModels(AccountModel accountModel, ushort gateId)
        {
            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                yield return model;
        }
    }
}