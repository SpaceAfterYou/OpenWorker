using Microsoft.EntityFrameworkCore;
using ow.Framework;
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

        public Characters(uint accountId, ushort gateId) : base(GetCharacterSlots())
        {
            Debug.Assert(accountId != 0);
            Debug.Assert(gateId != 0);

            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();
            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountId && c.GateId == gateId))
            {
                this[model.SlotId] = new(model);
            }

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;
        }

        public static Character[] GetCharacterSlots() =>
            Enumerable.Repeat<Character>(null, Defines.CharactersCount).ToArray();
    }
}
