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
    public sealed class Characters : List<Character>
    {
        public TimeSpan InitializeTime { get; init; }
        public Character LastSelected { get; set; }
        public Character Favorite { get; set; }

        public Characters(AccountModel accountModel, ushort gateId) : base(GetCharacterSlots())
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.GateId == gateId))
                this[model.SlotId] = new(model);

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;

            if (accountModel.LastSelectedCharacterId != -1)
                LastSelected = Find(c => c.Id == accountModel.LastSelectedCharacterId);

            if (accountModel.FavoriteCharacterId != -1)
                Favorite = Find(c => c.Id == accountModel.FavoriteCharacterId);
        }

        public static Character[] GetCharacterSlots() =>
            Enumerable.Repeat<Character>(null, Defines.CharactersCount).ToArray();
    }
}