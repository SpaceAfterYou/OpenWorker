using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game.Entities
{
    public sealed class SpecialOptionEntity : List<SpecialOptionEntity>
    {
        public SpecialOptionEntity this[SpecialOption index] => this[(int)index];

        public SpecialOptionEntity ShorterCooldown => this[(int)SpecialOption.ShorterCooldown];
        public SpecialOptionEntity AdditionalDamagePvP => this[(int)SpecialOption.AdditionalDamagePvP];
        public SpecialOptionEntity ReducedDamagePvP => this[(int)SpecialOption.ReducedDamagePvP];
        public SpecialOptionEntity AdditionalDamageGeneralMobs => this[(int)SpecialOption.AdditionalDamageGeneralMobs];
        public SpecialOptionEntity AdditionalDamageBossNamedMobs => this[(int)SpecialOption.AdditionalDamageBossNamedMobs];
        public SpecialOptionEntity Unknown5 => this[(int)SpecialOption.Unknown5];
        public SpecialOptionEntity Unknown6 => this[(int)SpecialOption.Unknown6];
        public SpecialOptionEntity AdditionalDamageAerialAttacks => this[(int)SpecialOption.AdditionalDamageAerialAttacks];
        public SpecialOptionEntity AdditionalDamageCapsized => this[(int)SpecialOption.AdditionalDamageCapsized];
        public SpecialOptionEntity ReducedDamageGeneralMobs => this[(int)SpecialOption.ReducedDamageGeneralMobs];
        public SpecialOptionEntity ReducedDamageBossNamedMobs => this[(int)SpecialOption.ReducedDamageBossNamedMobs];
        public SpecialOptionEntity Unknown11 => this[(int)SpecialOption.Unknown11];
        public SpecialOptionEntity Unknown12 => this[(int)SpecialOption.Unknown12];
        public SpecialOptionEntity SuperArmourBreak => this[(int)SpecialOption.SuperArmourBreak];
        public SpecialOptionEntity SgCostReduction => this[(int)SpecialOption.SgCostReduction];
        public SpecialOptionEntity ExpFromEnemy => this[(int)SpecialOption.ExpFromEnemy];
        public SpecialOptionEntity ZennyFromEnemy => this[(int)SpecialOption.ZennyFromEnemy];
        public SpecialOptionEntity SoulVaporGain => this[(int)SpecialOption.SoulVaporGain];
        public SpecialOptionEntity Unknown18 => this[(int)SpecialOption.Unknown18];
        public SpecialOptionEntity Unknown19 => this[(int)SpecialOption.Unknown19];
        public SpecialOptionEntity Unknown20 => this[(int)SpecialOption.Unknown20];
        public SpecialOptionEntity OnKillEffectRecoveryHp => this[(int)SpecialOption.OnKillEffectRecoveryHp];

        public SpecialOptionEntity() : base(Enumerable.Range(0, typeof(Stat).GetEnumValues().Cast<byte>().Max()).Select(id => new SpecialOptionEntity { Id = (SpecialOption)id, Value = 0.0f }))
        {
        }
    }
}