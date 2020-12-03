using Core.DatabaseSystem.Characters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GateService.Systems.GameSystem
{
    public sealed class Characters : List<Character>
    {
        public TimeSpan InitializeTime { get; init; }
        public uint LastSelectedId { get; set; }

        public Characters(uint accountId, byte gateId) : base()
        {
            Debug.Assert(accountId != 0);
            Debug.Assert(gateId != 0);

            Stopwatch stopwatch = new();
            stopwatch.Start();

            using CharacterContext context = new();
            foreach (var model in context.Character.AsNoTracking().Where(c => c.AccountId == accountId && c.GateId == gateId))
            {
                this[model.SlotId] = new(model);
            }

            stopwatch.Stop();

            InitializeTime = stopwatch.Elapsed;
        }

        public static Character[] GetCharacterSlots() =>
            Enumerable.Range(0, SoulWorker.Constants.CharactersCount).Select<int, Character>(_ => null).ToArray();
    }
}