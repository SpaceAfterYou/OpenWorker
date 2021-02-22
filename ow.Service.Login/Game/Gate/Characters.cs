using ow.Framework.Database.Accounts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList : ConcurrentDictionary<int, CharacterList.Character>
    {
        internal static readonly CharacterList Empty = new();

        internal readonly TimeSpan InitializeTime;
        internal Character? Favorite { get; set; }
        internal Character? LastSelected { get; set; }

        internal CharacterList(AccountModel accountModel, Stopwatch stopwatch, IEnumerable<KeyValuePair<int, Character>> characters) :
            base(characters)
        {
            if (accountModel.LastSelectedCharacter != -1 && TryGetValue(accountModel.LastSelectedCharacter, out Character? last))
            {
                LastSelected = last;
            }

            if (accountModel.FavoriteCharacter != -1 && TryGetValue(accountModel.FavoriteCharacter, out Character? favorite))
            {
                Favorite = favorite;
            }

            InitializeTime = stopwatch.Elapsed;
        }

        private CharacterList()
        {
        }
    }
}