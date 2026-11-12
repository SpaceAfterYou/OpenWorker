using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Buffs;

/// <summary>
/// The buffs of everyone in one maze instance: the players who are in it and the monsters something has buffed. Every buff
/// goes through <see cref="Give(Entity, int, Entity)"/> or one of the hit paths that call it; the maze steps them on its
/// clock. What a buff shows (06/14 to 06/17, the stats it changes, the life and gauges it gives or takes) goes to every
/// member of the instance.
/// </summary>
public sealed partial class MazeBuffs
{
    private const byte FlatHealth = 13;
    private const byte HealthShare = 14;
    private const byte FlatSoulGauge = 17;
    private const byte SoulGaugeShare = 18;
    private const byte FlatStamina = 21;
    private const byte StaminaShare = 22;
    private const float Percent = 0.01f;

    private readonly World _ecs;
    private readonly LuaCreatureManager _creatures;
    private readonly BuffCatalog _catalog;
    private readonly CombatTables _tables;
    private readonly PlayerGauges? _gauges;

    private readonly Dictionary<Entity, BuffBook> _books = [];

    /// <summary>
    /// The stats each carrier was last shown with, so a change tells only what changed.
    /// </summary>
    private readonly Dictionary<Entity, CombatStats> _shown = [];

    private readonly HashSet<Entity> _held = [];
    private readonly Dictionary<Entity, int> _taunted = [];
    private readonly List<BuffChange> _changes = [];
    private readonly List<(Entity Monster, int Owner)> _fallen = [];

    public MazeBuffs(World ecs, LuaCreatureManager creatures, BuffCatalog catalog, CombatTables tables, PlayerGauges? gauges = null, BuffManager? manager = null)
    {
        _manager = manager;
        _ecs = ecs;
        _creatures = creatures;
        _catalog = catalog;
        _tables = tables;
        _gauges = gauges;

        creatures.Buffs = this;
    }

    /// <summary>
    /// A monster died of what a buff took from it, with the actor that gave the buff. The maze counts the kill.
    /// </summary>
    public event Action<Entity, int>? MonsterKilled;

    internal Random Random { get; } = new();

    /// <summary>
    /// How many carriers have a book: the players and the monsters something has buffed.
    /// </summary>
    public int Carriers => _books.Count;

    /// <summary>
    /// A player comes into the maze: from now on they carry buffs, which their stats read.
    /// </summary>
    public void Attach(Entity player)
    {
        if (_books.ContainsKey(player) || !_ecs.IsAlive(player))
        {
            return;
        }

        var book = new BuffBook();

        _books.Add(player, book);

        // A player of the server has the component from the start; one made by hand gets it now.
        if (_ecs.Has<BuffBookComponent>(player))
        {
            _ecs.Set(player, new BuffBookComponent { Book = book });
        }
        else
        {
            _ecs.Add(player, new BuffBookComponent { Book = book });
        }

        if (_ecs.Has<PersonInfoComponent>(player))
        {
            _shown[player] = _tables.GetPlayer(_ecs, player);
        }

        // The buffs the person carried from the server it came from, or through a relog, with the time they had left.
        if (_ecs.Has<ActorComponent>(player))
        {
            foreach (var saved in BuffMemory.Saved(_ecs, player))
            {
                BuffRules.Restore(book, _catalog, saved.Buff, saved.Time, saved.Count, Carrier(player), _changes);
            }

            if (_changes.Count > 0)
            {
                Process(player, book);
            }
        }

        StartSources(player);
    }

    /// <summary>
    /// A player leaves the maze: what they carry stays behind, as the server they go to keeps no buffs of the maze.
    /// </summary>
    public void Detach(Entity player)
    {
        // What the person carries goes with it to the next server, but for what its skills give wherever it is.
        if (_books.TryGetValue(player, out var carried))
        {
            BuffMemory.Remember(_ecs, player, carried, _sources.TryGetValue(player, out var sources) ? sources.Sources.Given : []);
        }

        StopSources(player);
        _books.Remove(player);
        _shown.Remove(player);

        if (_ecs.IsAlive(player) && _ecs.Has<BuffBookComponent>(player))
        {
            _ecs.Set(player, new BuffBookComponent());
        }
    }

    /// <summary>
    /// Takes every buff away from someone: a monster that goes home starts over.
    /// </summary>
    public void ClearAll(Entity carrier)
    {
        if (!_books.TryGetValue(carrier, out var book) || book.IsEmpty || !_ecs.IsAlive(carrier))
        {
            return;
        }

        BuffRules.Clear(book, _catalog, null, Carrier(carrier), _changes);
        Process(carrier, book);
    }

    /// <summary>
    /// What the buffs of someone do to the damage it deals and takes.
    /// </summary>
    public BuffRates RatesOf(Entity carrier)
    {
        return _books.TryGetValue(carrier, out var book) ? book.Rates : default;
    }

    /// <summary>
    /// The damage of a hit once the buffs of both sides have had their say: a boss is hurt more by whoever has a buff against
    /// bosses, a player is hurt less by a boss when it has a buff against that, and all the damage of someone with a buff of
    /// all attacks grows (a monster hitting a player is the one pair that this does not touch).
    /// </summary>
    public int Adjust(Entity attacker, Entity target, int damage)
    {
        if (damage <= 0 || !_ecs.IsAlive(attacker) || !_ecs.IsAlive(target))
        {
            return damage;
        }

        var own = RatesOf(attacker);
        var other = RatesOf(target);
        var total = (float)damage;

        if (IsBoss(target))
        {
            total += total * own.BossAttackAdd;
        }
        else if (IsBoss(attacker) && IsPlayer(target))
        {
            total -= total * other.BossAttackedDown;
        }

        if (IsPlayer(attacker) || !IsPlayer(target))
        {
            total += total * own.AllAttackAdd;
        }

        return Math.Max(1, (int)total);
    }

    /// <summary>
    /// A hit landed for <paramref name="damage"/>: the target with a buff of reflection loses the share of it once more, the
    /// attacker with a buff of blood takes a share of it back as life (and so does the one who gave the target a blood debuff,
    /// or a friend of it), and an attacker with a buff of soul absorption fills its soul gauge. A hit that reaches several targets
    /// counts them by <paramref name="index"/>: the buffs name how many of them count.
    /// </summary>
    public void AfterHit(Entity attacker, Entity target, int index, int damage, in CombatStats targetStats)
    {
        if (damage <= 0 || !_ecs.IsAlive(attacker) || !_ecs.IsAlive(target))
        {
            return;
        }

        var own = RatesOf(attacker);
        var other = RatesOf(target);

        if (other.Reflect > 0f)
        {
            Lose(target, (int)(damage * other.Reflect));
        }

        var blood = own.Blood > 0f && (own.BloodTargets == 0 || index < own.BloodTargets) ? own.Blood : 0f;

        if (other.BloodDebuff > 0f && Friendly(other.BloodDebuffOwner, attacker))
        {
            blood += other.BloodDebuff;
        }

        if (blood > 0f)
        {
            Gain(attacker, (int)(damage * blood));
        }

        if (own.SoulAbsorb > 0f && (own.SoulAbsorbTargets == 0 || index < own.SoulAbsorbTargets) && IsPlayer(attacker) && _gauges is not null)
        {
            var maximum = _tables.GetPlayer(_ecs, attacker).SoulGaugeMax;

            _gauges.Absorb(attacker, maximum * targetStats.SoulGaugeAbsorbRate * own.SoulAbsorb, maximum);
        }
    }

    private bool IsBoss(Entity e) => _ecs.IsAlive(e) && _ecs.Get<ActorComponent>(e).Type == ActorType.Monster && _ecs.TryGet<MonsterCombatComponent>(e, out var combat) && combat.IsBoss;

    /// <summary>
    /// Whether the actor that gave a buff is on the side of someone: the players of a maze are friends, as the monsters are.
    /// </summary>
    private bool Friendly(int owner, Entity other)
    {
        if (owner == 0)
        {
            return false;
        }

        if (_ecs.Get<ActorComponent>(other) == owner)
        {
            return true;
        }

        foreach (var (carrier, _) in _books)
        {
            if (_ecs.IsAlive(carrier) && _ecs.Get<ActorComponent>(carrier) == owner)
            {
                return IsPlayer(carrier) == IsPlayer(other);
            }
        }

        foreach (var member in _creatures.Members)
        {
            if (_ecs.IsAlive(member) && _ecs.Get<ActorComponent>(member) == owner)
            {
                return IsPlayer(other);
            }
        }

        return false;
    }

    /// <summary>
    /// Takes life from someone without ever bringing it down to nothing: what the buff of reflection does.
    /// </summary>
    private void Lose(Entity carrier, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        var actor = _ecs.Get<ActorComponent>(carrier);

        if (IsPlayer(carrier))
        {
            if (_creatures.Vitals.Health(carrier) <= amount)
            {
                return;
            }

            var left = _creatures.Vitals.Hurt(carrier, amount);

            _creatures.Broadcast(new PersonUpdateStatListResponse { Actor = actor, Stats = [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_HP_CUR, fValue = left }] });

            return;
        }

        var creature = _ecs.Get<CreatureComponent>(carrier);

        if (creature.Health <= amount)
        {
            return;
        }

        _ecs.Set(carrier, creature with { Health = creature.Health - amount });
        _creatures.Broadcast(new MonsterUpdateStatListResponse { Actor = actor, Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_CUR, statValue = creature.Health - amount }] });
    }

    private void Gain(Entity carrier, int amount)
    {
        if (amount > 0 && _ecs.IsAlive(carrier))
        {
            Restore(carrier, _ecs.Get<ActorComponent>(carrier), IsPlayer(carrier), new BuffChange(BuffChangeKind.Restored, 0, Amount: amount, Stat: FlatHealth));
        }
    }

    /// <summary>
    /// A monster is gone from the maze.
    /// </summary>
    public void Forget(Entity creature)
    {
        _auras.Remove(creature);
        _taunted.Remove(creature);
        _books.Remove(creature);
        _shown.Remove(creature);
        _held.Remove(creature);
    }

    /// <summary>
    /// Gives a buff from a source (a player, a monster, a helper: anything with an actor) to a target in the maze, by the rules
    /// of a buff a hit gives: its chance, its stand-by time, the buffs it bundles. True when it was given or waits to begin.
    /// </summary>
    public bool Give(Entity target, int buff, Entity source)
    {
        var owner = _ecs.IsAlive(source) && _ecs.Has<ActorComponent>(source) ? (int)_ecs.Get<ActorComponent>(source) : 0;

        return Give(target, buff, owner);
    }

    /// <param name="owner">The actor that gives it, on the wire; zero for the target itself.</param>
    /// <param name="roll">Whether the chance of the buff decides, as for a hit; a script gives it for sure.</param>
    public bool Give(Entity target, int buff, int owner, bool roll = true)
    {
        if (BookOf(target) is not { } book || !IsUp(target))
        {
            return false;
        }

        var given = roll
            ? BuffRules.Give(book, _catalog, buff, owner, Carrier(target, withResistances: true), Random, _changes)
            : _catalog.TryGet(buff, out var definition) && BuffRules.Apply(book, _catalog, definition, owner, Carrier(target), _changes);

        Process(target, book);
        Flush();

        return given;
    }

    /// <summary>
    /// An attack that hurts nobody: one for the side of the attacker gives its buff to its side, one for its enemies to the
    /// targets it reached.
    /// </summary>
    public void GiveFromAttack(Entity attacker, in AttackTrigger trigger, IReadOnlyList<Entity> targets)
    {
        if (trigger.ForAllies)
        {
            GiveToAllies(attacker, trigger);
        }
        else
        {
            GiveFromHit(attacker, trigger, targets);
        }
    }

    /// <summary>
    /// The buff of an attack reaches every target the attack hit.
    /// </summary>
    public void GiveFromHit(Entity attacker, in AttackTrigger trigger, IReadOnlyList<Entity> targets)
    {
        if ((trigger.Buff <= 0 && trigger.Aura <= 0) || !_ecs.IsAlive(attacker))
        {
            return;
        }

        var owner = (int)_ecs.Get<ActorComponent>(attacker);

        foreach (var target in targets)
        {
            if (trigger.Buff > 0)
            {
                Give(target, trigger.Buff, owner);
            }

            if (trigger.Aura > 0)
            {
                SetAura(target, trigger.Aura);
            }
        }
    }

    /// <summary>
    /// An attack for the side of the attacker gives its buff to the attacker, its friends or its party, by its target type:
    /// the attacker wherever it is, the others when the area of the attack reaches them. It hurts nobody.
    /// </summary>
    public void GiveToAllies(Entity attacker, in AttackTrigger trigger)
    {
        if ((trigger.Buff <= 0 && trigger.Aura <= 0) || !_ecs.IsAlive(attacker))
        {
            return;
        }

        var owner = (int)_ecs.Get<ActorComponent>(attacker);

        if (trigger.TargetType == AttackTrigger.Self)
        {
            GiveOne(attacker, trigger, owner);

            return;
        }

        var place = _ecs.Get<WorldComponent>(attacker);
        var area = new AttackArea(trigger, place.Position, place.Rotation);
        var player = _ecs.Get<ActorComponent>(attacker).Type == ActorType.User;
        var includesAttacker = trigger.IncludesAttacker;

        IEnumerable<Entity> side = player
            ? _creatures.Members.Where(e => _ecs.IsAlive(e) && area.Touches(_creatures.Vitals.PositionOf(e), PlayerVitals.BodyRadius, PlayerVitals.BodyHeight)).ToArray()
            : _creatures.FindMonsters((position, body) => area.Touches(position, body.BodyRadius, body.BodyHeight));

        foreach (var target in side)
        {
            if (target != attacker || includesAttacker)
            {
                GiveOne(target, trigger, owner);
            }
        }
    }

    /// <summary>
    /// What an attack gives one target it reached: its buff, and the aura it names.
    /// </summary>
    private void GiveOne(Entity target, in AttackTrigger trigger, int owner)
    {
        if (trigger.Buff > 0)
        {
            Give(target, trigger.Buff, owner);
        }

        if (trigger.Aura > 0)
        {
            SetAura(target, trigger.Aura);
        }
    }

    /// <summary>
    /// Time passes for every buff of the maze.
    /// </summary>
    public void Step(float seconds)
    {
        if (_books.Count == 0)
        {
            return;
        }

        StepSources(seconds);

        foreach (var (carrier, book) in _books)
        {
            if (book.IsEmpty || !_ecs.IsAlive(carrier))
            {
                continue;
            }

            BuffRules.Step(book, _catalog, seconds, Carrier(carrier), _changes);

            if (_changes.Count > 0)
            {
                Process(carrier, book);
            }
        }

        Flush();
    }

    /// <summary>
    /// The stats of a monster with its buffs on them.
    /// </summary>
    public CombatStats StatsOf(Entity monster, in CombatStats stats)
    {
        return _books.TryGetValue(monster, out var book) ? book.Apply(stats) : stats;
    }

    /// <summary>
    /// How someone takes hits: its own defense, or what a buff makes of it when that is stronger.
    /// </summary>
    public DefenseType DefenseOf(Entity carrier, DefenseType defense)
    {
        return _books.TryGetValue(carrier, out var book) && book.Defense is { } buffed && buffed > defense ? buffed : defense;
    }

    /// <summary>
    /// Stunned, paralysed, asleep or frozen: it neither moves nor attacks.
    /// </summary>
    public bool IsHeld(Entity carrier)
    {
        return _books.TryGetValue(carrier, out var book) && book.IsHeld;
    }

    /// <summary>
    /// A player fell: death takes the buffs that are not kept on death.
    /// </summary>
    public void OnDeath(Entity carrier)
    {
        if (!_books.TryGetValue(carrier, out var book) || book.IsEmpty)
        {
            return;
        }

        BuffRules.ClearOnDeath(book, _catalog, Carrier(carrier), _changes);
        Process(carrier, book);
    }

    /// <summary>
    /// Tells a player who comes into a maze that runs the buffs everyone in it carries.
    /// </summary>
    public void ShowTo(ServerSessionComponent session)
    {
        foreach (var (carrier, book) in _books)
        {
            if (!_ecs.IsAlive(carrier))
            {
                continue;
            }

            var actor = _ecs.Get<ActorComponent>(carrier);

            foreach (var buff in book.Buffs)
            {
                session.Send(new SkillUpdateBuffResponse { Target = actor, Buff = (short)buff.Buff, Time = Time(buff), Count = buff.Count, Owner = buff.Owner });
            }
        }
    }

    private BuffBook? BookOf(Entity target)
    {
        if (_books.TryGetValue(target, out var book))
        {
            return book;
        }

        // A monster has a book from the first buff it is given.
        if (!_ecs.IsAlive(target) || !_ecs.Has<MonsterCombatComponent>(target) || _ecs.Get<ActorComponent>(target).Type != ActorType.Monster)
        {
            return null;
        }

        book = new BuffBook();
        _books.Add(target, book);

        return book;
    }

    private bool IsPlayer(Entity carrier) => _ecs.TryGet<ActorComponent>(carrier, out var actor) && actor.Type == ActorType.User;

    /// <summary>
    /// The dead take no buff.
    /// </summary>
    private bool IsUp(Entity carrier)
    {
        if (!_ecs.IsAlive(carrier))
        {
            return false;
        }

        if (IsPlayer(carrier))
        {
            return !_ecs.Has<AbilityComponent>(carrier) || _creatures.Vitals.IsAlive(carrier);
        }

        return _ecs.Get<CreatureComponent>(carrier).Health > 0;
    }

    /// <param name="withResistances">What the carrier resists is looked up too, which only the buffs that are given need.</param>
    private BuffCarrier Carrier(Entity carrier, bool withResistances = false)
    {
        var actor = (int)_ecs.Get<ActorComponent>(carrier);

        if (IsPlayer(carrier))
        {
            var vitals = _creatures.Vitals;

            if (!_ecs.Has<AbilityComponent>(carrier))
            {
                return new BuffCarrier(actor, 1, 1, false);
            }

            // What the player resists is in the stats it was last told, which the buffs it carries are part of.
            var resist = withResistances && _ecs.TryGet<ShownStatsComponent>(carrier, out var shown) ? shown.Final.Resist : default;

            return new BuffCarrier(actor, vitals.Health(carrier), vitals.MaxHealth(carrier), vitals.IsProtected(carrier), Resist: resist);
        }

        var combat = _ecs.Get<MonsterCombatComponent>(carrier);
        var monsterResist = withResistances && _books.TryGetValue(carrier, out var book) ? book.Apply(combat.Stats).Resist : combat.Stats.Resist;

        return new BuffCarrier(actor, _ecs.Get<CreatureComponent>(carrier).Health, (int)combat.Stats.MaxHealth, combat.Defense == DefenseType.Invincible,
            Resist: monsterResist, Boss: combat.IsBoss);
    }

    private static float Time(ActiveBuff buff) => buff.Definition.Lasts ? 0f : Math.Max(0f, buff.Remaining);

    /// <summary>
    /// Tells the clients what the rules changed and changes life, gauges and stats with it.
    /// </summary>
    private void Process(Entity carrier, BuffBook book)
    {
        var actor = _ecs.Get<ActorComponent>(carrier);
        var player = actor.Type == ActorType.User;
        var recount = false;

        // Death can add changes while the list is read, so it is read by index.
        for (var index = 0; index < _changes.Count; index++)
        {
            var change = _changes[index];

            switch (change.Kind)
            {
                case BuffChangeKind.Updated:
                    _creatures.Broadcast(new SkillUpdateBuffResponse { Target = actor, Buff = (short)change.Buff, Time = Math.Max(0f, change.Time), Count = change.Count, Owner = change.Owner });
                    break;

                case BuffChangeKind.Changed:
                    _creatures.Broadcast(new SkillBuffChangeBroadcastResponse { Target = actor, Buff = (short)change.Buff, NewBuff = (short)change.Other, Time = Math.Max(0f, change.Time), Count = change.Count, Owner = change.Owner });
                    break;

                case BuffChangeKind.Deleted:
                    _creatures.Broadcast(new SkillBuffDeleteBroadcastResponse { Actor = actor, Buff = (short)change.Buff, ExecuteOutSkill = change.CallOut });
                    break;

                case BuffChangeKind.Hurt:
                    Hurt(carrier, book, actor, player, change);
                    break;

                case BuffChangeKind.Restored:
                    Restore(carrier, actor, player, change);
                    break;

                case BuffChangeKind.Recounted:
                    recount = true;
                    break;
            }
        }

        _changes.Clear();

        if (recount && _ecs.IsAlive(carrier))
        {
            ShowStats(carrier, book, actor, player);
        }
    }

    private void Hurt(Entity carrier, BuffBook book, ActorComponent actor, bool player, in BuffChange change)
    {
        if (!_ecs.IsAlive(carrier) || change.Amount <= 0)
        {
            return;
        }

        int left;

        if (player)
        {
            if (!_creatures.Vitals.IsAlive(carrier))
            {
                return;
            }

            left = _creatures.Vitals.Hurt(carrier, change.Amount);
        }
        else
        {
            var creature = _ecs.Get<CreatureComponent>(carrier);

            if (creature.Health <= 0)
            {
                return;
            }

            left = Math.Max(0, creature.Health - change.Amount);
            _ecs.Set(carrier, creature with { Health = left });

            // The monster is angered at the one who gave the burn and wakes, as a blow would wake it.
            _creatures.BodyOf(carrier)?.Machine.OnHurtOverTime(change.Owner, change.Amount);
        }

        _creatures.Broadcast(new SkillBuffDamageBroadcastResponse { Target = actor, Buff = (short)change.Buff, Damage = change.Amount, Health = left, Owner = change.Owner });

        if (left > 0)
        {
            return;
        }

        if (player)
        {
            _creatures.Broadcast(new PersonDieResponse { Victim = actor, Attacker = default, PvpKillCount = 0 });

            BuffRules.ClearOnDeath(book, _catalog, Carrier(carrier), _changes);
            _creatures.Services?.Wear?.Died(carrier);
        }
        else
        {
            _fallen.Add((carrier, change.Owner));
        }
    }

    private void Restore(Entity carrier, ActorComponent actor, bool player, in BuffChange change)
    {
        if (!_ecs.IsAlive(carrier))
        {
            return;
        }

        switch (change.Stat)
        {
            case FlatHealth or HealthShare when player:
                var vitals = _creatures.Vitals;
                var before = vitals.Health(carrier);
                var amount = change.Stat == FlatHealth ? change.Amount : (int)(vitals.MaxHealth(carrier) * change.Amount * Percent);
                var health = vitals.Heal(carrier, amount);

                if (health != before)
                {
                    _creatures.Broadcast(new PersonUpdateStatListResponse { Actor = actor, Stats = [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_HP_CUR, fValue = health }] });
                }

                break;

            case FlatHealth or HealthShare:
                var creature = _ecs.Get<CreatureComponent>(carrier);
                var maximum = (int)_ecs.Get<MonsterCombatComponent>(carrier).Stats.MaxHealth;
                var gain = change.Stat == FlatHealth ? change.Amount : (int)(maximum * change.Amount * Percent);
                var healed = Math.Min(maximum, creature.Health + gain);

                if (creature.Health > 0 && healed != creature.Health)
                {
                    _ecs.Set(carrier, creature with { Health = healed });
                    _creatures.Broadcast(new MonsterUpdateStatListResponse { Actor = actor, Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_CUR, statValue = healed }] });
                }

                break;

            case FlatSoulGauge or SoulGaugeShare when player && _gauges is not null:
                _gauges.Restore(carrier, SkillCostKind.SoulGauge, change.Amount, change.Stat == SoulGaugeShare);
                break;

            case FlatStamina or StaminaShare when player && _gauges is not null:
                _gauges.Restore(carrier, SkillCostKind.Stamina, change.Amount, change.Stat == StaminaShare);
                break;
        }
    }

    /// <summary>
    /// The stats changed by the buffs: a player is told its own (03/34), everyone is told those of a monster (17/12), and a
    /// monster that is now held stops what it does.
    /// </summary>
    private void ShowStats(Entity carrier, BuffBook book, ActorComponent actor, bool player)
    {
        if (player && !_ecs.Has<PersonInfoComponent>(carrier))
        {
            return;
        }

        // The stats of a player are told by the one place that keeps what the player was told.
        if (player && _creatures.Services?.Stats is { } stats)
        {
            stats.Send(carrier);
            return;
        }

        var now = player ? _tables.GetPlayer(_ecs, carrier) : book.Apply(_ecs.Get<MonsterCombatComponent>(carrier).Stats);
        var before = _shown.TryGetValue(carrier, out var shown) ? shown : player ? now : _ecs.Get<MonsterCombatComponent>(carrier).Stats;
        var changed = CombatStats.Differences(before, now);

        _shown[carrier] = now;

        if (changed.Count > 0)
        {
            if (player)
            {
                if (_ecs.TryGet<ServerSessionComponent>(carrier, out var session))
                {
                    session.Send(new PersonUpdateStatListResponse
                    {
                        Actor = actor,
                        Stats = changed.Select(e => new ST_STAT { wStatID = (short)e.Type, fValue = e.Value }).ToArray()
                    });
                }

                if (before.MaxHealth != now.MaxHealth)
                {
                    _creatures.Vitals.Resize(carrier, (int)Math.Max(1f, now.MaxHealth));
                }
            }
            else
            {
                _creatures.Broadcast(new MonsterUpdateStatListResponse
                {
                    Actor = actor,
                    Stats = changed.Select(e => new StatKeyValuePair { byIndex = e.Type, statValue = e.Value }).ToArray()
                });
            }
        }

        if (player)
        {
            return;
        }

        // A monster with a taunt on it turns on the one who gave it.
        var taunt = book.Rates.Taunt;

        if (taunt != 0 && (!_taunted.TryGetValue(carrier, out var turned) || turned != taunt))
        {
            _taunted[carrier] = taunt;
            _creatures.Provoke(carrier, taunt);
        }
        else if (taunt == 0 && _taunted.Remove(carrier))
        {
            _creatures.EndTaunt(carrier);
        }

        if (book.IsHeld)
        {
            // A monster that is stunned, frozen or held stops where it is and drops what it was doing.
            if (_held.Add(carrier))
            {
                _creatures.Hold(carrier);
            }
        }
        else
        {
            _held.Remove(carrier);
        }
    }

    private void Flush()
    {
        if (_fallen.Count == 0)
        {
            return;
        }

        var fallen = _fallen.ToArray();

        _fallen.Clear();

        foreach (var (monster, owner) in fallen)
        {
            MonsterKilled?.Invoke(monster, owner);
        }
    }
}
