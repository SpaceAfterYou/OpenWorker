using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Characters : Dictionary<int, Character>
    {
        public TimeSpan InitializeTime { get; }
        public Character? Favorite { get; set; }
        public Character? LastSelected { get; set; }

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId))
                if (!TryAdd(model.Id, new(model, tables)))
                    NetworkUtils.DropSession();

            if (accountModel.LastSelectedCharacter != -1 && TryGetValue(accountModel.LastSelectedCharacter, out Character? last))
                LastSelected = last;

            if (accountModel.FavoriteCharacter != -1 && TryGetValue(accountModel.FavoriteCharacter, out Character? favorite))
                Favorite = favorite;

            stopwatch.Stop();
            InitializeTime = stopwatch.Elapsed;
        }

        private static IEnumerable<CharacterModel> GetCharacterModels(AccountModel accountModel, ushort gateId)
        {
            using CharacterContext context = new();

            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                yield return model;
        }
    }
}