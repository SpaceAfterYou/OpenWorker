using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game.Entities
{
    public sealed class SpecialOptionEntity : List<SpecialOptionEntity>
    {
        public SpecialOptionEntity this[SpecialOption index] => this[(int)index];

        public SpecialOptionEntity ShorterCooldown => this[SpecialOption.ShorterCooldown];
        public SpecialOptionEntity AdditionalDamagePvP => this[SpecialOption.AdditionalDamagePvP];
        public SpecialOptionEntity ReducedDamagePvP => this[SpecialOption.ReducedDamagePvP];
        public SpecialOptionEntity AdditionalDamageGeneralMobs => this[SpecialOption.AdditionalDamageGeneralMobs];
        public SpecialOptionEntity AdditionalDamageBossNamedMobs => this[SpecialOption.AdditionalDamageBossNamedMobs];
        public SpecialOptionEntity Unknown5 => this[SpecialOption.Unknown5];
        public SpecialOptionEntity Unknown6 => this[SpecialOption.Unknown6];
        public SpecialOptionEntity AdditionalDamageAerialAttacks => this[SpecialOption.AdditionalDamageAerialAttacks];
        public SpecialOptionEntity AdditionalDamageCapsized => this[SpecialOption.AdditionalDamageCapsized];
        public SpecialOptionEntity ReducedDamageGeneralMobs => this[SpecialOption.ReducedDamageGeneralMobs];
        public SpecialOptionEntity ReducedDamageBossNamedMobs => this[SpecialOption.ReducedDamageBossNamedMobs];
        public SpecialOptionEntity Unknown11 => this[SpecialOption.Unknown11];
        public SpecialOptionEntity Unknown12 => this[SpecialOption.Unknown12];
        public SpecialOptionEntity SuperArmourBreak => this[SpecialOption.SuperArmourBreak];
        public SpecialOptionEntity SgCostReduction => this[SpecialOption.SgCostReduction];
        public SpecialOptionEntity ExpFromEnemy => this[SpecialOption.ExpFromEnemy];
        public SpecialOptionEntity ZennyFromEnemy => this[SpecialOption.ZennyFromEnemy];
        public SpecialOptionEntity SoulVaporGain => this[SpecialOption.SoulVaporGain];
        public SpecialOptionEntity Unknown18 => this[SpecialOption.Unknown18];
        public SpecialOptionEntity Unknown19 => this[SpecialOption.Unknown19];
        public SpecialOptionEntity Unknown20 => this[SpecialOption.Unknown20];
        public SpecialOptionEntity OnKillEffectRecoveryHp => this[SpecialOption.OnKillEffectRecoveryHp];

        public SpecialOptionEntity() : base(Enumerable.Range(0, typeof(Stat).GetEnumValues().Cast<byte>().Max()).Select(id => new SpecialOptionEntity { Id = (SpecialOption)id, Value = 0.0f }))
        {
        }
    }
}