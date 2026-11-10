using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua.Buffs;

/// <summary>
/// The buffs a person gets without being hit by anything, as the original gives them: the lasting buffs of its passive skills,
/// the cards of its quick slots that fire on a condition or every few seconds, the buffs of its combos, and the auras that give a
/// buff to those around.
/// </summary>
public sealed partial class MazeBuffs
{
    /// <summary>How often the passive skills are matched against the book and the cards that work while they are in a slot fire.</summary>
    private const float SourceInterval = 1f;

    /// <summary>How often the life and the soul gauge are looked at for the cards that wait for them to fall or rise.</summary>
    private const float ConditionInterval = 2f;

    private const float ComboWindow = 3f;
    private const int MostComboHits = 5;
    private const int ComboRepeat = 100;

    /// <summary>The target type of an aura that is for its carrier alone.</summary>
    private const byte AuraSelf = 1;

    // The conditions a card names (the original's passive conditions).
    public const byte OnAttack = 1;
    public const byte OnCriticalAttack = 6;
    public const byte OnMissedAttack = 11;
    public const byte OnDamagedMissed = 14;
    public const byte OnDamaged = 16;
    public const byte OnCriticallyDamaged = 21;
    public const byte OnSkill = 26;
    public const byte WhileEquipped = 46;
    public const byte OnComboHundred = 48;
    public const byte OnComboTwenty = 49;
    public const byte OnLifeUnderFifth = 50;
    public const byte OnLifeUnderHalf = 54;

    private readonly BuffManager? _manager;
    private readonly Dictionary<Entity, PersonSources> _sources = [];
    private readonly Dictionary<Entity, AuraState> _auras = [];
    private float _now;

    /// <summary>
    /// What a person has from its passive skills and cards, and what it has done toward a combo.
    /// </summary>
    private sealed class PersonSources
    {
        public SkillInfoValue[]? Learned { get; set; }

        public int[]? Slots { get; set; }

        public List<AkashicRecordsRow> Cards { get; } = [];

        /// <summary>The buffs the passive skills and the clothing of the person give.</summary>
        public SourceBuffs Sources { get; } = new();

        public float Clock { get; set; }

        public float Check { get; set; }

        /// <summary>When a card may fire again, by its cooldown group.</summary>
        public Dictionary<int, float> Ready { get; } = [];

        public int Combo { get; set; }

        public float LastHit { get; set; } = float.NegativeInfinity;
    }

    private sealed class AuraState(AuraRow row, float check, float left)
    {
        public AuraRow Row { get; } = row;

        public float Check { get; set; } = check;

        /// <summary>Seconds it has left, zero for one that lasts until it is replaced.</summary>
        public float Left { get; set; } = left;
    }

    private void StartSources(Entity player)
    {
        _sources[player] = new PersonSources();

        Reconcile(player, _sources[player]);
    }

    private void StopSources(Entity player)
    {
        _sources.Remove(player);
        _auras.Remove(player);
    }

    /// <summary>
    /// A person or a monster puts an aura on itself: its buff goes to those around it now (when the aura is checked from the
    /// start) and again at every check, until the aura runs out or is replaced. An aura that is not one takes the aura away. The
    /// maze is told. False when nothing changed.
    /// </summary>
    public bool SetAura(Entity source, int aura)
    {
        if (!_ecs.IsAlive(source) || !_ecs.Has<ActorComponent>(source))
        {
            return false;
        }

        var current = _auras.GetValueOrDefault(source);

        if ((aura == 0 && current is null) || (current is not null && current.Row.Id == aura))
        {
            return false;
        }

        _creatures.Broadcast(new SkillAuraUpdateBroadcastResponse { Target = _ecs.Get<ActorComponent>(source), Aura = (short)Math.Clamp(aura, 0, short.MaxValue) });

        if (_manager is null || !_manager.TryGetAura(aura, out var row))
        {
            _auras.Remove(source);

            return false;
        }

        if (row.CheckType == 0)
        {
            ApplyAura(source, row);
        }

        _auras[source] = new AuraState(row, row.CheckMilliseconds * 0.001f, row.DurationMilliseconds * 0.001f);

        return true;
    }

    /// <summary>
    /// The aura of someone, zero for none.
    /// </summary>
    public int AuraOf(Entity source) => _auras.TryGetValue(source, out var state) ? state.Row.Id : 0;

    /// <summary>
    /// Gives the buff of an aura to the ones it is for, who are close enough to its carrier: the carrier alone for an aura that is
    /// for itself, otherwise whoever the target type names (the carrier and its friends, its friends, its enemies, everyone else).
    /// </summary>
    private void ApplyAura(Entity source, in AuraRow row)
    {
        if (!_ecs.IsAlive(source) || !_catalog.TryGet(row.Buff & ushort.MaxValue, out _))
        {
            return;
        }

        var owner = (int)_ecs.Get<ActorComponent>(source);

        if (row.Target == AuraSelf)
        {
            Give(source, row.Buff & ushort.MaxValue, owner);

            return;
        }

        var origin = PlaceOf(source);
        var reach = (float)row.Distance * row.Distance;

        foreach (var other in Everyone())
        {
            if (_ecs.IsAlive(other) && IsUp(other) && AuraIsFor(source, row.Target, other) && Vector3.DistanceSquared(origin, PlaceOf(other)) < reach)
            {
                Give(other, row.Buff & ushort.MaxValue, owner);
            }
        }
    }

    /// <summary>
    /// Whom an aura or an attack of a target type reaches: 1 the carrier, 2 and 7 the carrier and its friends, 3 its enemies, 4
    /// everyone but the carrier, 6 and 8 its friends.
    /// </summary>
    private bool AuraIsFor(Entity source, int targetType, Entity other)
    {
        var same = IsPlayer(source) == IsPlayer(other);

        return targetType switch
        {
            1 => source == other,
            2 or 7 => source == other || same,
            3 => source != other && !same,
            4 => source != other,
            6 or 8 => source != other && same,
            _ => false
        };
    }

    private Vector3 PlaceOf(Entity carrier) => _ecs.Get<WorldComponent>(carrier).Position;

    /// <summary>
    /// Every player of the maze and every monster that is in it.
    /// </summary>
    private IEnumerable<Entity> Everyone()
    {
        return [.. _creatures.Members, .. _creatures.FindMonsters((_, _) => true)];
    }

    private void StepSources(float seconds)
    {
        _now += seconds;

        foreach (var (player, state) in _sources)
        {
            if (!_ecs.IsAlive(player))
            {
                continue;
            }

            state.Clock += seconds;
            state.Check += seconds;

            // A change to the skills shows at once, anything else (a death took a buff, a card came out of cooldown) within a second.
            if (state.Clock >= SourceInterval || !ReferenceEquals(state.Learned, SkillsOf(player)))
            {
                state.Clock = 0f;

                Reconcile(player, state);
                FireCards(player, state, WhileEquipped);
            }

            if (state.Check >= ConditionInterval)
            {
                state.Check = 0f;

                CheckLife(player, state);
            }
        }

        foreach (var (carrier, aura) in _auras.ToArray())
        {
            if (!_ecs.IsAlive(carrier))
            {
                _auras.Remove(carrier);

                continue;
            }

            if (aura.Left > 0f)
            {
                aura.Left -= seconds;

                if (aura.Left <= 0f)
                {
                    SetAura(carrier, 0);

                    continue;
                }
            }

            aura.Check -= seconds;

            if (aura.Check <= 0f)
            {
                aura.Check = aura.Row.CheckMilliseconds * 0.001f;

                ApplyAura(carrier, aura.Row);
            }
        }
    }

    private SkillInfoValue[]? SkillsOf(Entity player) => _ecs.TryGet<SkillLearnedComponent>(player, out var learned) ? learned.Values : null;

    /// <summary>
    /// Makes the buffs the sources of a person give what its skills and its clothing say: the ones it has are on it, the ones it
    /// no longer has are taken away, and a buff a death took is given again.
    /// </summary>
    private void Reconcile(Entity player, PersonSources state)
    {
        if (_manager is null || !_ecs.Has<ActorComponent>(player) || BookOf(player) is not { } book || !IsUp(player))
        {
            return;
        }

        state.Learned = SkillsOf(player);

        var inventory = _ecs.TryGet<InventoryComponent>(player, out var bag) ? bag.Value : null;

        BuffSources.Reconcile(book, _catalog, _manager.Passives, state.Learned, inventory, state.Sources, Carrier(player), _changes);

        if (_changes.Count > 0)
        {
            Process(player, book);
        }

        RefreshCards(player, state);
    }

    private void RefreshCards(Entity player, PersonSources state)
    {
        var slots = _ecs.TryGet<InventoryComponent>(player, out var inventory) ? inventory.Value?.QuickSlotCards : null;

        if (_manager is null || ReferenceEquals(state.Slots, slots))
        {
            return;
        }

        state.Slots = slots;
        state.Cards.Clear();

        if (slots is not null)
        {
            _manager.Passives.CardsOf(slots, state.Cards);
        }
    }

    /// <summary>
    /// Something happened to a person that its passive cards wait for (it hit, it was hit, it used a skill, its life is low): the
    /// cards that name the condition give their buff, by their chance and when they are not cooling down.
    /// </summary>
    public void OnCondition(Entity player, byte condition)
    {
        if (_sources.TryGetValue(player, out var state))
        {
            FireCards(player, state, condition);
        }
    }

    private void FireCards(Entity player, PersonSources state, byte condition)
    {
        RefreshCards(player, state);

        foreach (var card in state.Cards)
        {
            if (card.InvokeCondition != condition || !IsUp(player))
            {
                continue;
            }

            if (Random.Next(Certain) >= card.InvokeRate)
            {
                continue;
            }

            var group = card.CooldownGroup != 0 ? card.CooldownGroup : card.Id;

            if (state.Ready.TryGetValue(group, out var ready) && ready > _now)
            {
                continue;
            }

            if (Give(player, card.Buff & ushort.MaxValue, owner: 0, roll: false) && _ecs.TryGet<ServerSessionComponent>(player, out var session))
            {
                session.Send(new SkillAkashicRecordResponse { Unknown = 0, Card = default(SkillAkashicRecordValue) with { Akashic = (uint)card.Id, Player = _ecs.Get<ActorComponent>(player) } });
            }

            state.Ready[group] = _now + card.Cooldown * 0.001f;
        }
    }

    private const int Certain = 10000;

    private void CheckLife(Entity player, PersonSources state)
    {
        if (!_ecs.Has<AbilityComponent>(player) || !IsUp(player))
        {
            return;
        }

        var maximum = _creatures.Vitals.MaxHealth(player);

        if (maximum <= 0)
        {
            return;
        }

        var share = _creatures.Vitals.Health(player) / (float)maximum;

        if (share <= 0.2f)
        {
            FireCards(player, state, OnLifeUnderFifth);
        }

        if (share <= 0.5f)
        {
            FireCards(player, state, OnLifeUnderHalf);
        }
    }

    /// <summary>
    /// An attack of a player landed on <paramref name="targets"/> targets: the combo goes on when it comes within three seconds of
    /// the last one, and every table entry it passes gives its buffs. Past the last entry the last entry repeats every hundred hits.
    /// </summary>
    public void OnHits(Entity player, int targets)
    {
        if (_manager is not { Combos.Count: > 0 } || !_sources.TryGetValue(player, out var state))
        {
            return;
        }

        var hits = Math.Min(targets, MostComboHits);
        var before = state.Combo;
        var last = _manager.Combos[^1];

        if (state.LastHit + ComboWindow <= _now)
        {
            state.Combo = hits > 0 ? hits - 1 : 0;
        }
        else
        {
            var next = state.Combo + hits;

            if (last.Id >= next || state.Combo <= 0)
            {
                foreach (var combo in _manager.Combos)
                {
                    if (combo.Id > next)
                    {
                        break;
                    }

                    if (combo.Id > state.Combo)
                    {
                        ApplyCombo(player, combo);

                        break;
                    }
                }
            }
            else if (state.Combo % ComboRepeat > next % ComboRepeat)
            {
                ApplyCombo(player, last);
            }

            state.Combo += hits;
        }

        state.LastHit = _now;

        if (state.Combo / ComboRepeat > before / ComboRepeat)
        {
            FireCards(player, state, OnComboHundred);
        }

        if (state.Combo / 20 > before / 20)
        {
            FireCards(player, state, OnComboTwenty);
        }
    }

    /// <summary>
    /// What a combo gives: its fixed buff, and a few of its random buffs, picked one after another by their weights.
    /// </summary>
    private void ApplyCombo(Entity player, in ComboBuffRow combo)
    {
        if (combo.FixedBuff != 0)
        {
            Give(player, combo.FixedBuff & ushort.MaxValue, owner: 0, roll: false);
        }

        Span<short> buffs = [combo.RandomBuff1, combo.RandomBuff2, combo.RandomBuff3, combo.RandomBuff4, combo.RandomBuff5, combo.RandomBuff6, combo.RandomBuff7, combo.RandomBuff8];
        Span<short> rates = [combo.RandomRate1, combo.RandomRate2, combo.RandomRate3, combo.RandomRate4, combo.RandomRate5, combo.RandomRate6, combo.RandomRate7, combo.RandomRate8];

        for (var picked = 0; picked < combo.RandomCount; picked++)
        {
            var total = 0;

            foreach (var rate in rates)
            {
                total += Math.Max((short)0, rate);
            }

            if (total <= 0)
            {
                break;
            }

            var roll = Random.Next(total);

            for (var index = 0; index < rates.Length; index++)
            {
                if (rates[index] <= 0)
                {
                    continue;
                }

                if (roll >= rates[index])
                {
                    roll -= rates[index];

                    continue;
                }

                Give(player, buffs[index] & ushort.MaxValue, owner: 0, roll: false);

                // Each buff of the row is picked at most once.
                rates[index] = 0;

                break;
            }
        }
    }
}
