using DefaultEcs;
using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
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

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                this[model.Slot].Set<EntityCharacter>(new(model, tables));

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;

            if (accountModel.LastSelectedCharacter != -1)
                LastSelected = Find(c => c.Get<EntityCharacter>().Id == accountModel.LastSelectedCharacter);

            if (accountModel.FavoriteCharacter != -1)
                Favorite = Find(c => c.Get<EntityCharacter>().Id == accountModel.FavoriteCharacter);
        }

        private static Entity[] GetCharacterSlots(World _entities) =>
            Enumerable.Repeat(_entities.CreateEntity(), Defines.CharactersCount).ToArray();
    }
}