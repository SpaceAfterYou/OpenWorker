using DefaultEcs;
using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using ow.Framework.Game.Storage;
using ow.Service.Gate.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Characters : List<Entity>, IDisposable
    {
        public TimeSpan InitializeTime { get; init; }
        public EntityCharacter LastSelected { get; set; }
        public EntityCharacter Favorite { get; set; }

        private readonly World _entities = new();

        public void Dispose() => _entities.Dispose();

        public void Remove(int slot)
        {
            EntityCharacter character = this[slot].Get<EntityCharacter>();
            if (LastSelected != null && character.Id == LastSelected.Id)
            {
                int index = FindIndex(c => c.Has<EntityCharacter>());
                if (index != -1) LastSelected = this[index].Get<EntityCharacter>();
            }

            if (character.Id == Favorite?.Id) Favorite = null;

            this[slot].Dispose();
            this[slot] = _entities.CreateEntity();
        }

        public void Create(CharacterModel model, BinTables tables)
        {
            this[model.Slot].Set(LastSelected = new(model, tables));
            this[model.Slot].Set<IStatsEntity>(new GateStatsEntity());
            this[model.Slot].Set<IStorageEntity>(new GateStorageEntity());
        }

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables, World entities) : base(GetCharacterSlots(entities))
        {
            _entities = entities;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId))
            {
                this[model.Slot].Set<EntityCharacter>(new(model, tables));
                this[model.Slot].Set<IStatsEntity>(new GateStatsEntity());
                this[model.Slot].Set<IStorageEntity>(new GateStorageEntity(model));
            }

            if (accountModel.LastSelectedCharacter != -1)
            {
                int index = FindIndex(c => c.Get<EntityCharacter>().Id == accountModel.LastSelectedCharacter);
                if (index != -1) LastSelected = this[index].Get<EntityCharacter>();
            }

            if (accountModel.FavoriteCharacter != -1)
            {
                int index = FindIndex(c => c.Get<EntityCharacter>().Id == accountModel.FavoriteCharacter);
                if (index != -1) Favorite = this[index].Get<EntityCharacter>();
            }

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
            Enumerable.Repeat(0, Defines.CharactersCount).Select(_ => _entities.CreateEntity()).ToArray();
    }
}