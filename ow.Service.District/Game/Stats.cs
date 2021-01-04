using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class Stats : List<Stats.Entity>
    {
        public sealed class Entity
        {
            public CharacterStat Id { get; init; }
            public float Value { get; set; }
        }

        public Entity this[CharacterStat index] => this[(int)index];

        public Entity Unknown0 => this[CharacterStat.Unknown0];
        public Entity CurrentHp => this[CharacterStat.CurrentHp];
        public Entity CurrentSg => this[CharacterStat.CurrentSg];
        public Entity Unknown3 => this[CharacterStat.Unknown3];
        public Entity Unknown4 => this[CharacterStat.Unknown4];
        public Entity Unknown5 => this[CharacterStat.Unknown5];
        public Entity Unknown6 => this[CharacterStat.Unknown6];
        public Entity Unknown7 => this[CharacterStat.Unknown7];
        public Entity Unknown8 => this[CharacterStat.Unknown8];
        public Entity Unknown9 => this[CharacterStat.Unknown9];
        public Entity MaxHp => this[CharacterStat.MaxHp];
        public Entity Unknown11 => this[CharacterStat.Unknown11];
        public Entity MaxSg => this[CharacterStat.MaxSg];
        public Entity Unknown12 => this[CharacterStat.Unknown12];
        public Entity Unknown13 => this[CharacterStat.Unknown13];
        public Entity Stamina => this[CharacterStat.Stamina];
        public Entity StaminaRegeneration => this[CharacterStat.StaminaRegeneration];
        public Entity Unknown16 => this[CharacterStat.Unknown16];
        public Entity Unknown17 => this[CharacterStat.Unknown17];
        public Entity MoveSpeed => this[CharacterStat.MoveSpeed];
        public Entity AttackSpeed => this[CharacterStat.AttackSpeed];
        public Entity MinAttackDamage => this[CharacterStat.MinAttackDamage];
        public Entity MaxAttackDamage => this[CharacterStat.MaxAttackDamage];
        public Entity Unknown22 => this[CharacterStat.Unknown22];
        public Entity Unknown23 => this[CharacterStat.Unknown23];
        public Entity Defence => this[CharacterStat.Defence];
        public Entity Unknown25 => this[CharacterStat.Unknown25];
        public Entity Accuracy => this[CharacterStat.Accuracy];
        public Entity Unknown27 => this[CharacterStat.Unknown27];
        public Entity PartialDamage => this[CharacterStat.PartialDamage];
        public Entity CritChance => this[CharacterStat.CritChance];
        public Entity Unknown30 => this[CharacterStat.Unknown30];
        public Entity CritResistance => this[CharacterStat.CritResistance];
        public Entity Unknown32 => this[CharacterStat.Unknown32];
        public Entity Unknown33 => this[CharacterStat.Unknown33];
        public Entity Unknown34 => this[CharacterStat.Unknown34];
        public Entity CritDamage => this[CharacterStat.CritDamage];
        public Entity Unknown36 => this[CharacterStat.Unknown36];
        public Entity Unknown37 => this[CharacterStat.Unknown37];
        public Entity DamageReduction => this[CharacterStat.DamageReduction];
        public Entity Unknown39 => this[CharacterStat.Unknown39];
        public Entity Unknown40 => this[CharacterStat.Unknown40];
        public Entity Unknown41 => this[CharacterStat.Unknown41];
        public Entity Unknown42 => this[CharacterStat.Unknown42];
        public Entity Evade => this[CharacterStat.Evade];
        public Entity Unknown44 => this[CharacterStat.Unknown44];
        public Entity Unknown45 => this[CharacterStat.Unknown45];
        public Entity Unknown46 => this[CharacterStat.Unknown46];
        public Entity ArmourBreak => this[CharacterStat.ArmourBreak];
        public Entity Unknown48 => this[CharacterStat.Unknown48];
        public Entity FireResistance => this[CharacterStat.FireResistance];
        public Entity PoisonResistance => this[CharacterStat.PoisonResistance];
        public Entity ElectricResistance => this[CharacterStat.ElectricResistance];
        public Entity BleedResistance => this[CharacterStat.BleedResistance];
        public Entity Stun1Resistance => this[CharacterStat.Stun1Resistance];
        public Entity ParalysisResistance => this[CharacterStat.ParalysisResistance];
        public Entity SleepResistance => this[CharacterStat.SleepResistance];
        public Entity FrostResistance => this[CharacterStat.FrostResistance];
        public Entity SilenceResistance => this[CharacterStat.SilenceResistance];
        public Entity VulnResistance => this[CharacterStat.VulnResistance];
        public Entity Stun2Resistance => this[CharacterStat.Stun2Resistance];
        public Entity ConfusedResistance => this[CharacterStat.ConfusedResistance];
        public Entity Unknown61 => this[CharacterStat.Unknown61];
        public Entity Unknown62 => this[CharacterStat.Unknown62];
        public Entity VirtueDamage => this[CharacterStat.VirtueDamage];
        public Entity SinDamage => this[CharacterStat.SinDamage];
        public Entity GraceDamage => this[CharacterStat.GraceDamage];
        public Entity HateDamage => this[CharacterStat.HateDamage];
        public Entity SolaceDamage => this[CharacterStat.SolaceDamage];
        public Entity TormentDamage => this[CharacterStat.TormentDamage];
        public Entity VirtueResistance => this[CharacterStat.VirtueResistance];
        public Entity SinResistance => this[CharacterStat.SinResistance];
        public Entity GraceResistance => this[CharacterStat.GraceResistance];
        public Entity HateResistance => this[CharacterStat.HateResistance];
        public Entity SolaceResistance => this[CharacterStat.SolaceResistance];
        public Entity TormentResistance => this[CharacterStat.TormentResistance];
        public Entity ManicBalanceDamage => this[CharacterStat.ManicBalanceDamage];
        public Entity ManicBalanceResistance => this[CharacterStat.ManicBalanceResistance];

        public Stats() : base(Enumerable.Range(0, typeof(CharacterStat).GetEnumValues().Cast<ushort>().Max()).Select(id => new Entity { Id = (CharacterStat)id, Value = 100.0f }))
        {
        }
    }
}