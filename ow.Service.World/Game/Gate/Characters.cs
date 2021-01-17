using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static ow.Service.World.Game.Gate.Characters;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters : Dictionary<int, CEntity>
    {
        public TimeSpan InitializeTime { get; }
        public CEntity? Favorite { get; set; }
        public CEntity? LastSelected { get; set; }

        public Characters(AccountModel accountModel, ushort gateId, CharacterContext characterContext, ItemContext itemContext)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId, characterContext))
                if (!TryAdd(model.Id, new(model, itemContext)))
                    NetworkUtils.DropBadAction();

            if (accountModel.LastSelectedCharacter != -1 && TryGetValue(accountModel.LastSelectedCharacter, out CEntity? last))
                LastSelected = last;

            if (accountModel.FavoriteCharacter != -1 && TryGetValue(accountModel.FavoriteCharacter, out CEntity? favorite))
                Favorite = favorite;

            stopwatch.Stop();
            InitializeTime = stopwatch.Elapsed;
        }

        private static IEnumerable<CharacterModel> GetCharacterModels(AccountModel accountModel, ushort gateId, CharacterContext context)
        {
            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                yield return model;
        }
    }
}