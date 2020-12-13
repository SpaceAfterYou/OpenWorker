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
    internal sealed class Characters : List<EntityCharacter>
    {
        public TimeSpan InitializeTime { get; init; }
        public EntityCharacter LastSelected { get; set; }
        public EntityCharacter Favorite { get; set; }

        public Characters(AccountModel accountModel, ushort gateId, BinTables binTable) : base(GetCharacterSlots())
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.GateId == gateId))
                this[model.SlotId] = new(model, binTable);

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;

            if (accountModel.LastSelectedCharacter != -1)
                LastSelected = Find(c => c.Id == accountModel.LastSelectedCharacter);

            if (accountModel.FavoriteCharacter != -1)
                Favorite = Find(c => c.Id == accountModel.FavoriteCharacter);
        }

        public static EntityCharacter[] GetCharacterSlots() =>
            Enumerable.Repeat<EntityCharacter>(null, Defines.CharactersCount).ToArray();
    }
}