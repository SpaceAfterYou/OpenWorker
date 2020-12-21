using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class SpecialOptions : List<SpecialOptions.Entity>
    {
        public sealed class Entity
        {
            public SpecialOption Id { get; init; }
            public float Value { get; set; }
        }

        public Entity this[SpecialOption index] => this[(int)index];

        public Entity ShorterCooldown => this[SpecialOption.ShorterCooldown];
        public Entity AdditionalDamagePvP => this[SpecialOption.AdditionalDamagePvP];
        public Entity ReducedDamagePvP => this[SpecialOption.ReducedDamagePvP];
        public Entity AdditionalDamageGeneralMobs => this[SpecialOption.AdditionalDamageGeneralMobs];
        public Entity AdditionalDamageBossNamedMobs => this[SpecialOption.AdditionalDamageBossNamedMobs];
        public Entity Unknown5 => this[SpecialOption.Unknown5];
        public Entity Unknown6 => this[SpecialOption.Unknown6];
        public Entity AdditionalDamageAerialAttacks => this[SpecialOption.AdditionalDamageAerialAttacks];
        public Entity AdditionalDamageCapsized => this[SpecialOption.AdditionalDamageCapsized];
        public Entity ReducedDamageGeneralMobs => this[SpecialOption.ReducedDamageGeneralMobs];
        public Entity ReducedDamageBossNamedMobs => this[SpecialOption.ReducedDamageBossNamedMobs];
        public Entity Unknown11 => this[SpecialOption.Unknown11];
        public Entity Unknown12 => this[SpecialOption.Unknown12];
        public Entity SuperArmourBreak => this[SpecialOption.SuperArmourBreak];
        public Entity SgCostReduction => this[SpecialOption.SgCostReduction];
        public Entity ExpFromEnemy => this[SpecialOption.ExpFromEnemy];
        public Entity ZennyFromEnemy => this[SpecialOption.ZennyFromEnemy];
        public Entity SoulVaporGain => this[SpecialOption.SoulVaporGain];
        public Entity Unknown18 => this[SpecialOption.Unknown18];
        public Entity Unknown19 => this[SpecialOption.Unknown19];
        public Entity Unknown20 => this[SpecialOption.Unknown20];
        public Entity OnKillEffectRecoveryHp => this[SpecialOption.OnKillEffectRecoveryHp];

        public SpecialOptions() : base(Enumerable.Range(0, typeof(SpecialOption).GetEnumValues().Cast<byte>().Max()).Select(id => new Entity { Id = (SpecialOption)id, Value = 0.0f }))
        {
        }
    }
}