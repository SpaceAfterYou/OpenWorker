using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Lua.Buffs;

public sealed partial class MazeBuffs
{
    /// <summary>
    /// Monsters that the damage sent back by gear has killed. They die at the next step of the buffs, not in the middle of the
    /// blow of their own that sent the damage back.
    /// </summary>
    private readonly List<(Entity Monster, int Owner)> _fallenLater = [];

    /// <summary>
    /// A blow landed (it did not miss) for <paramref name="damage"/>: the special effects of gear on both sides answer it. The
    /// attacker gives the one hit its debuff and itself its buff, and its stamina gauge fills by its share; the one hit gives
    /// the attacker its debuff and itself its buff, and sends its share of the damage back to the attacker.
    /// </summary>
    public void AfterLandedBlow(Entity attacker, Entity target, SpecialEffects own, SpecialEffects other, int damage)
    {
        if (damage <= 0 || !_ecs.IsAlive(attacker) || !_ecs.IsAlive(target) || own.IsNone && other.IsNone)
        {
            return;
        }

        GiveByEffect(target, own.Of(SpecialEffect.AttackDebuff), attacker);
        GiveByEffect(attacker, other.Of(SpecialEffect.DamagedDebuff), target);
        GiveByEffect(attacker, own.Of(SpecialEffect.AttackBuff), attacker);
        GiveByEffect(target, other.Of(SpecialEffect.DamagedBuff), target);

        var stamina = own.Of(SpecialEffect.StaminaOnAttack);

        if (stamina > 0f && IsPlayer(attacker))
        {
            _gauges?.Restore(attacker, SkillCostKind.Stamina, stamina, share: true);
        }

        var reflected = (int)(damage * other.Of(SpecialEffect.Reflection) * Percent);

        if (reflected > 0 && _ecs.IsAlive(attacker) && !IsInvincible(attacker))
        {
            SendBack(attacker, target, reflected);
        }
    }

    /// <summary>
    /// A special effect names a buff by its number; zero names none.
    /// </summary>
    private void GiveByEffect(Entity carrier, float buff, Entity source)
    {
        if (buff >= 1f && buff <= ushort.MaxValue && _ecs.IsAlive(carrier))
        {
            Give(carrier, (int)buff, source);
        }
    }

    private bool IsInvincible(Entity carrier)
    {
        if (IsPlayer(carrier))
        {
            return _creatures.IsShielded(carrier);
        }

        return _ecs.TryGet<MonsterCombatComponent>(carrier, out var combat) && DefenseOf(carrier, _creatures.FrameDefenseOf(carrier, combat.Defense)) == DefenseType.Invincible;
    }

    /// <summary>
    /// The attacker loses the life the one it hit sends back, as the damage of a buff without a number; it can fall of it.
    /// </summary>
    private void SendBack(Entity attacker, Entity from, int amount)
    {
        if (BookOf(attacker) is not { } book)
        {
            return;
        }

        var fallen = _fallen.Count;

        _changes.Add(new BuffChange(BuffChangeKind.Hurt, 0, Owner: _ecs.Get<ActorComponent>(from), Amount: amount));
        Process(attacker, book);

        if (_fallen.Count > fallen)
        {
            _fallenLater.AddRange(_fallen.Skip(fallen));
            _fallen.RemoveRange(fallen, _fallen.Count - fallen);
        }
    }
}
