using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Characters : List<Character>
    {
        public TimeSpan InitializeTime { get; init; }
        public Character LastSelected { get; set; }
        public Character Favorite { get; set; }

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables) : base(GetCharacterSlots())
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                this[model.Slot] = new(model, tables);

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;

            if (accountModel.LastSelectedCharacter != -1)
                LastSelected = Find(c => c.Entity.Id == accountModel.LastSelectedCharacter);

            if (accountModel.FavoriteCharacter != -1)
                Favorite = Find(c => c.Entity.Id == accountModel.FavoriteCharacter);
        }

        public static Character[] GetCharacterSlots() =>
            Enumerable.Repeat<Character>(null, Defines.CharactersCount).ToArray();
    }
}