using SoulCore.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class SpecialOptions : List<SpecialOptions.Entity>
    {
        public sealed class Entity
        {
            internal SpecialOption Id { get; init; }
            internal float Value { get; set; }
        }

        internal Entity this[SpecialOption index] => this[(int)index];

        internal Entity ShorterCooldown => this[SpecialOption.ShorterCooldown];
        internal Entity AdditionalDamagePvP => this[SpecialOption.AdditionalDamagePvP];
        internal Entity ReducedDamagePvP => this[SpecialOption.ReducedDamagePvP];
        internal Entity AdditionalDamageGeneralMobs => this[SpecialOption.AdditionalDamageGeneralMobs];
        internal Entity AdditionalDamageBossNamedMobs => this[SpecialOption.AdditionalDamageBossNamedMobs];
        internal Entity Unknown5 => this[SpecialOption.Unknown5];
        internal Entity Unknown6 => this[SpecialOption.Unknown6];
        internal Entity AdditionalDamageAerialAttacks => this[SpecialOption.AdditionalDamageAerialAttacks];
        internal Entity AdditionalDamageCapsized => this[SpecialOption.AdditionalDamageCapsized];
        internal Entity ReducedDamageGeneralMobs => this[SpecialOption.ReducedDamageGeneralMobs];
        internal Entity ReducedDamageBossNamedMobs => this[SpecialOption.ReducedDamageBossNamedMobs];
        internal Entity Unknown11 => this[SpecialOption.Unknown11];
        internal Entity Unknown12 => this[SpecialOption.Unknown12];
        internal Entity SuperArmourBreak => this[SpecialOption.SuperArmourBreak];
        internal Entity SgCostReduction => this[SpecialOption.SgCostReduction];
        internal Entity ExpFromEnemy => this[SpecialOption.ExpFromEnemy];
        internal Entity ZennyFromEnemy => this[SpecialOption.ZennyFromEnemy];
        internal Entity SoulVaporGain => this[SpecialOption.SoulVaporGain];
        internal Entity Unknown18 => this[SpecialOption.Unknown18];
        internal Entity Unknown19 => this[SpecialOption.Unknown19];
        internal Entity Unknown20 => this[SpecialOption.Unknown20];
        internal Entity OnKillEffectRecoveryHp => this[SpecialOption.OnKillEffectRecoveryHp];

        internal SpecialOptions() : base(Enumerable.Range(0, typeof(SpecialOption).GetEnumValues().Cast<byte>().Max() + 1).Select(id => new Entity { Id = (SpecialOption)id, Value = 0.0f }))
        {
            Debug.Assert(this.Last().Id == SpecialOption.OnKillEffectRecoveryHp);
        }
    }
}