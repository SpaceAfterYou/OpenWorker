using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// What a change of the buffs of someone means for the clients and for their life and gauges.
/// </summary>
public enum BuffChangeKind : byte
{
    /// <summary>A buff began, was renewed or stacked (06/14).</summary>
    Updated,

    /// <summary>A buff of a group was replaced by another of the group (06/16).</summary>
    Changed,

    /// <summary>A buff ended or was taken away (06/15).</summary>
    Deleted,

    /// <summary>A buff took life away (06/17).</summary>
    Hurt,

    /// <summary>A buff gave back a current value: life, soul gauge or stamina, by the stat effect and its value.</summary>
    Restored,

    /// <summary>What the buffs add up to changed: stats, states, defense.</summary>
    Recounted
}

/// <param name="Other">The new buff of a change.</param>
/// <param name="Time">Seconds the buff has left, zero for one that lasts.</param>
/// <param name="Amount">The life a buff took, or the value of the stat it gave back.</param>
/// <param name="Stat">The stat effect a buff gave back.</param>
/// <param name="CallOut">Whether the buff ran out by itself, rather than being taken away.</param>
public readonly record struct BuffChange(
    BuffChangeKind Kind,
    ushort Buff,
    ushort Other = 0,
    float Time = 0f,
    byte Count = 0,
    int Owner = 0,
    int Amount = 0,
    byte Stat = 0,
    bool CallOut = false);

/// <summary>
/// What the carrier of a book is now, as the rules need it.
/// </summary>
/// <param name="Invincible">Nothing hurts it now: debuffs do not stick and buffs that hurt do nothing.</param>
/// <param name="Place">Where it is: <see cref="BuffDefinition.Maze"/> or <see cref="BuffDefinition.District"/>. A buff stays only where its row allows.</param>
/// <param name="Resist">What it resists of the states buffs put on it.</param>
/// <param name="Boss">A boss, which some buffs are not given to.</param>
public readonly record struct BuffCarrier(int Actor, int Health, int MaxHealth, bool Invincible, byte Place = BuffDefinition.Maze, Resistances Resist = default, bool Boss = false);

/// <summary>
/// The rules of buffs as the original server keeps them: how a buff is given, how a second one of its group stacks or
/// replaces the first, how buffs tick and run out, and what takes them away. Every change is written to a list for the one
/// who tells the clients and changes life and gauges.
/// </summary>
public static class BuffRules
{
    private const int Certain = 10000;
    private const byte MostStacks = 10;
    private const float Percent = 0.01f;

    /// <summary>
    /// The part of its time a buff adds when it stacks without starting its time again, by the stacks it has.
    /// </summary>
    private static readonly float[] AddedTime = [0.5f, 0.25f, 0.125f, 0.0625f, 0.03125f, 0.015625f, 0.015625f, 0.015625f, 0.015625f, 0.015625f];

    /// <summary>
    /// The stat effects that change a current value (life, soul gauge, stamina, soul vapor) instead of a stat: they give
    /// once when the buff begins and again at every tick, and nothing is taken back at the end.
    /// </summary>
    public static bool IsCurrentValue(byte stat) => stat is 13 or 14 or 17 or 18 or 21 or 22 or 27 or 28;

    /// <summary>
    /// A hit or a skill gives a buff: a buff that bundles others gives them instead, the chance of the buff is rolled, and a
    /// buff with a stand-by time waits for it. True when the buff (or one of its bundle) was given or is waiting.
    /// </summary>
    public static bool Give(BuffBook book, BuffCatalog catalog, int buff, int owner, in BuffCarrier carrier, Random random, List<BuffChange> changes, bool roll = true)
    {
        if (!catalog.TryGet(buff, out var definition) || !definition.AllowedIn(carrier.Place))
        {
            return false;
        }

        if (definition.Effect == BuffEffect.Bundle)
        {
            var given = false;

            foreach (var part in (ReadOnlySpan<int>)[definition.Stat1.Value, definition.Stat2.Value, definition.Stat3.Value])
            {
                if (part != 0 && part != buff && catalog.TryGet(part, out var inner) && inner.Effect != BuffEffect.Bundle)
                {
                    given |= Give(book, catalog, part, owner, carrier, random, changes, roll);
                }
            }

            return given;
        }

        // The chance of a buff is held back by what its carrier resists of the state; some buffs are not rolled at all.
        if (roll && definition.SuccessRate > 0 && !definition.IgnoresResistance
            && random.Next(Certain) >= (int)(definition.SuccessRate - definition.ResistanceOf(carrier.Resist)))
        {
            return false;
        }

        if (definition.StandBy > 0f)
        {
            var found = book.FindGroup(definition.Group, owner);

            if (found >= 0 && !CanStack(book.Active[found], definition))
            {
                return false;
            }

            book.Pending.Add(new PendingBuff(definition, owner));

            return true;
        }

        return Apply(book, catalog, definition, owner, carrier, changes);
    }

    /// <summary>
    /// Puts a buff on its carrier now, by the rules of its group: a new group takes a slot, a buff of the group already
    /// there is renewed, stacked or replaced, or the new one is refused.
    /// </summary>
    public static bool Apply(BuffBook book, BuffCatalog catalog, in BuffDefinition buff, int owner, in BuffCarrier carrier, List<BuffChange> changes)
    {
        if (book.Active.Count >= BuffBook.Capacity || !buff.AllowedIn(carrier.Place))
        {
            return false;
        }

        // What cannot be hurt takes no debuff, except one that gets through that; some buffs are not given to a boss.
        if (buff.Kind == BuffKind.Debuff && buff.SystemType != 3 && (carrier.Invincible || book.Defense == Combat.DefenseType.Invincible))
        {
            return false;
        }

        if (buff.SystemType == 1 && carrier.Boss)
        {
            return false;
        }

        // A frozen carrier takes no other state and nothing that hurts over time.
        if ((book.Control & BuffControl.Frozen) != 0 && ((buff.Controls && buff.Effect != BuffEffect.Freeze) || buff.Hurts))
        {
            return false;
        }

        if (owner == 0)
        {
            owner = carrier.Actor;
        }

        var index = book.FindGroup(buff.Group, owner);

        if (index < 0)
        {
            var added = new ActiveBuff(buff, owner);

            book.Active.Add(added);
            Begin(book, catalog, added, carrier, changes);
            index = book.Active.IndexOf(added);

            if (index < 0)
            {
                return true;
            }
        }
        else if (!Stack(book, catalog, index, buff, owner, carrier, changes))
        {
            return false;
        }

        // Sleep and freeze put out what burns, poisons, shocks and bleeds.
        if (buff.Effect is BuffEffect.Sleep or BuffEffect.Freeze)
        {
            for (var other = book.Active.Count - 1; other >= 0; other--)
            {
                if (book.Active[other].Definition.Effect is >= BuffEffect.Burn and <= BuffEffect.Bleed)
                {
                    Remove(book, catalog, other, false, carrier, changes);
                }
            }
        }

        index = book.FindGroup(buff.Group, owner);

        if (index >= 0)
        {
            var active = book.Active[index];

            changes.Add(new BuffChange(BuffChangeKind.Updated, active.Buff, Time: active.Remaining, Count: active.Count, Owner: active.Owner));
        }

        return true;
    }

    /// <summary>
    /// A second buff of a group meets the first. The grade must not be lower; a group that does not stack is only renewed;
    /// one that stacks to the top turns into its change buff; one buff of the group replaces another.
    /// </summary>
    private static bool Stack(BuffBook book, BuffCatalog catalog, int index, in BuffDefinition buff, int owner, in BuffCarrier carrier, List<BuffChange> changes)
    {
        var active = book.Active[index];

        if (!HasGrade(active, buff))
        {
            return false;
        }

        var most = Math.Min(buff.OverlapCount, MostStacks);

        if (most == 0)
        {
            return false;
        }

        if (most != 1)
        {
            if (active.Count + 1 >= most && buff.ChangeBuff != 0 && catalog.TryGet(buff.ChangeBuff, out var change))
            {
                Remove(book, catalog, index, false, carrier, changes);
                Apply(book, catalog, change, owner, carrier, changes);

                return false;
            }

            if (active.Count + 1 > most)
            {
                return false;
            }
        }

        active.Owner = owner;

        if (buff.OverlapType == 1)
        {
            // The time starts again.
            active.Remaining = buff.Duration;
            active.Count = most == 1 ? (byte)1 : (byte)(active.Count + 1);
        }
        else if (buff.OverlapType is 0 or 2)
        {
            if (most == 1)
            {
                active.Remaining = buff.Duration;
                active.Count = 1;
            }
            else
            {
                // A stack adds a part of the time, the smaller the more stacks there are.
                active.Remaining += buff.Duration * AddedTime[Math.Clamp(active.Count - 1, 0, AddedTime.Length - 1)];
                active.Count++;
            }
        }

        if (active.Buff == buff.Id)
        {
            // A group that stacks its values adds the values of the table with every stack, and gives them again.
            if (buff.OverlapType != 0 && most > 1)
            {
                active.Values[0] += buff.Stat1.Value;
                active.Values[1] += buff.Stat2.Value;
                active.Values[2] += buff.Stat3.Value;

                if (buff.Effect == BuffEffect.Stats)
                {
                    Restore(active, changes);
                }

                book.Recount();
                changes.Add(new BuffChange(BuffChangeKind.Recounted, active.Buff));
            }

            return true;
        }

        // Another buff of the group takes the place of the first.
        var old = active.Buff;

        active.Reset(buff, owner);
        changes.Add(new BuffChange(BuffChangeKind.Changed, old, buff.Id, active.Remaining, active.Count, active.Owner));
        Begin(book, catalog, active, carrier, changes);

        return true;
    }

    private static bool HasGrade(ActiveBuff active, in BuffDefinition buff)
    {
        return buff.Effect == BuffEffect.Taunt ? active.Definition.Grade < buff.Grade : active.Definition.Grade <= buff.Grade;
    }

    /// <summary>
    /// Whether a buff that waits for its stand-by time will still find room in its group.
    /// </summary>
    private static bool CanStack(ActiveBuff active, in BuffDefinition buff)
    {
        var most = Math.Min(buff.OverlapCount, MostStacks);

        return HasGrade(active, buff) && most > 0 && (active.Count + 1 <= most || most == 1);
    }

    /// <summary>
    /// What a buff does when it begins: the stats it changes, the values it gives back, what it takes away.
    /// </summary>
    private static void Begin(BuffBook book, BuffCatalog catalog, ActiveBuff buff, in BuffCarrier carrier, List<BuffChange> changes)
    {
        var definition = buff.Definition;

        switch (definition.Effect)
        {
            case BuffEffect.Stats:
                Restore(buff, changes);
                break;

            case BuffEffect.Cleanse:
                var effect = (BuffEffect)(int)buff.Values[0];

                for (var index = book.Active.Count - 1; index >= 0; index--)
                {
                    if (book.Active[index] != buff && book.Active[index].Definition.Effect == effect)
                    {
                        Remove(book, catalog, index, false, carrier, changes);
                    }
                }

                break;

            case BuffEffect.LifeCut:
                var cut = (int)(carrier.MaxHealth * buff.Values[0] * Percent);

                if (cut > 0)
                {
                    changes.Add(new BuffChange(BuffChangeKind.Hurt, buff.Buff, Owner: buff.Owner, Amount: cut));
                }

                break;
        }

        book.Recount();
        changes.Add(new BuffChange(BuffChangeKind.Recounted, buff.Buff));
    }

    /// <summary>
    /// A buff of stats gives back the current values it names.
    /// </summary>
    private static void Restore(ActiveBuff buff, List<BuffChange> changes)
    {
        for (var index = 0; index < 3; index++)
        {
            var stat = buff.Stat(index);

            if (stat.IsSet && IsCurrentValue(stat.Type) && buff.Values[index] > 0f)
            {
                changes.Add(new BuffChange(BuffChangeKind.Restored, buff.Buff, Owner: buff.Owner, Amount: (int)buff.Values[index], Stat: stat.Type));
            }
        }
    }

    /// <summary>
    /// Takes a buff away. One that ran out by itself calls its call-out buff.
    /// </summary>
    public static void Remove(BuffBook book, BuffCatalog catalog, int index, bool ranOut, in BuffCarrier carrier, List<BuffChange> changes)
    {
        if (index < 0 || index >= book.Active.Count)
        {
            return;
        }

        var buff = book.Active[index];

        book.Active.RemoveAt(index);
        changes.Add(new BuffChange(BuffChangeKind.Deleted, buff.Buff, Owner: buff.Owner, CallOut: ranOut));
        book.Recount();
        changes.Add(new BuffChange(BuffChangeKind.Recounted, buff.Buff));

        var next = buff.Definition.CallOutBuff;

        if (ranOut && next != 0 && next != buff.Buff && catalog.TryGet(next, out var callOut))
        {
            Apply(book, catalog, callOut, buff.Owner, carrier, changes);
        }
    }

    /// <summary>
    /// Lets time pass for the buffs of a carrier: the ones that wait begin, the ones that hurt or give back act at their
    /// ticks, and the ones that run out end and call what follows them.
    /// </summary>
    public static void Step(BuffBook book, BuffCatalog catalog, float seconds, in BuffCarrier carrier, List<BuffChange> changes)
    {
        if (seconds <= 0f || book.IsEmpty)
        {
            return;
        }

        for (var index = book.Pending.Count - 1; index >= 0; index--)
        {
            var pending = book.Pending[index];

            pending.Wait -= seconds;

            if (pending.Wait <= 0f)
            {
                book.Pending.RemoveAt(index);
                Apply(book, catalog, pending.Definition, pending.Owner, carrier, changes);
            }
        }

        // Buffs that come during the step (call-outs) begin to count from the next step.
        for (var index = 0; index < book.Active.Count; index++)
        {
            var buff = book.Active[index];
            var definition = buff.Definition;

            if (!definition.Lasts)
            {
                buff.Remaining -= seconds;
            }

            if (definition.Tick > 0f)
            {
                Tick(buff, seconds, carrier, changes);
            }
        }

        for (var index = book.Active.Count - 1; index >= 0; index--)
        {
            if (index < book.Active.Count && book.Active[index] is { Definition.Lasts: false, Remaining: <= 0f })
            {
                Remove(book, catalog, index, true, carrier, changes);
            }
        }
    }

    private static void Tick(ActiveBuff buff, float seconds, in BuffCarrier carrier, List<BuffChange> changes)
    {
        var definition = buff.Definition;

        if (definition.Hurts)
        {
            // An invincible carrier is not hurt and the ticks it skips are not owed.
            if (carrier.Invincible)
            {
                return;
            }

            buff.TickClock += seconds;

            var damage = definition.Stat1.Type switch
            {
                13 => (int)buff.Values[0],
                14 => (int)(carrier.Health * buff.Values[0] * Percent),
                16 => (int)(carrier.MaxHealth * buff.Values[0] * Percent),
                _ => 0
            };

            var total = 0;

            while (buff.TickClock > definition.Tick)
            {
                buff.TickClock -= definition.Tick;
                total += damage;
            }

            if (total > 0)
            {
                changes.Add(new BuffChange(BuffChangeKind.Hurt, buff.Buff, Owner: buff.Owner, Amount: total));
            }

            return;
        }

        if (definition.Effect != BuffEffect.Stats || carrier.Health <= 0)
        {
            return;
        }

        buff.TickClock += seconds;

        while (buff.TickClock > definition.Tick)
        {
            buff.TickClock -= definition.Tick;
            Restore(buff, changes);
        }
    }

    /// <summary>
    /// Death takes away every buff that is not kept on death.
    /// </summary>
    public static void ClearOnDeath(BuffBook book, BuffCatalog catalog, in BuffCarrier carrier, List<BuffChange> changes)
    {
        book.Pending.Clear();

        for (var index = book.Active.Count - 1; index >= 0; index--)
        {
            if ((book.Active[index].Definition.KeepOn & BuffDefinition.KeptOnDeath) == 0)
            {
                Remove(book, catalog, index, false, carrier, changes);
            }
        }
    }

    /// <summary>
    /// A carrier comes to a place: the buffs that may not be there are taken away.
    /// </summary>
    public static void ClearForPlace(BuffBook book, BuffCatalog catalog, in BuffCarrier carrier, List<BuffChange> changes)
    {
        for (var index = book.Pending.Count - 1; index >= 0; index--)
        {
            if (!book.Pending[index].Definition.AllowedIn(carrier.Place))
            {
                book.Pending.RemoveAt(index);
            }
        }

        for (var index = book.Active.Count - 1; index >= 0; index--)
        {
            if (!book.Active[index].Definition.AllowedIn(carrier.Place))
            {
                Remove(book, catalog, index, false, carrier, changes);
            }
        }
    }

    /// <summary>
    /// Takes away the buffs of one kind: leaving a maze takes the debuffs, a monster going home takes them all.
    /// </summary>
    public static void Clear(BuffBook book, BuffCatalog catalog, BuffKind? kind, in BuffCarrier carrier, List<BuffChange> changes)
    {
        book.Pending.Clear();

        for (var index = book.Active.Count - 1; index >= 0; index--)
        {
            if (kind is null || book.Active[index].Definition.Kind == kind)
            {
                Remove(book, catalog, index, false, carrier, changes);
            }
        }
    }
}
