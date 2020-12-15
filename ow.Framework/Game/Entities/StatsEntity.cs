using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game.Entities
{
    public sealed class StatsEntity : List<StatEntity>, IStatsEntity
    {
        public StatEntity this[Stat index] => this[(int)index];

        public StatEntity Unknown0 => this[Stat.Unknown0];
        public StatEntity CurrentHp => this[Stat.CurrentHp];
        public StatEntity CurrentSg => this[Stat.CurrentSg];
        public StatEntity Unknown3 => this[Stat.Unknown3];
        public StatEntity Unknown4 => this[Stat.Unknown4];
        public StatEntity Unknown5 => this[Stat.Unknown5];
        public StatEntity Unknown6 => this[Stat.Unknown6];
        public StatEntity Unknown7 => this[Stat.Unknown7];
        public StatEntity Unknown8 => this[Stat.Unknown8];
        public StatEntity Unknown9 => this[Stat.Unknown9];
        public StatEntity MaxHp => this[Stat.MaxHp];
        public StatEntity Unknown11 => this[Stat.Unknown11];
        public StatEntity MaxSg => this[Stat.MaxSg];
        public StatEntity Unknown12 => this[Stat.Unknown12];
        public StatEntity Unknown13 => this[Stat.Unknown13];
        public StatEntity Stamina => this[Stat.Stamina];
        public StatEntity StaminaRegeneration => this[Stat.StaminaRegeneration];
        public StatEntity Unknown16 => this[Stat.Unknown16];
        public StatEntity Unknown17 => this[Stat.Unknown17];
        public StatEntity MoveSpeed => this[Stat.MoveSpeed];
        public StatEntity AttackSpeed => this[Stat.AttackSpeed];
        public StatEntity MinAttackDamage => this[Stat.MinAttackDamage];
        public StatEntity MaxAttackDamage => this[Stat.MaxAttackDamage];
        public StatEntity Unknown22 => this[Stat.Unknown22];
        public StatEntity Unknown23 => this[Stat.Unknown23];
        public StatEntity Defence => this[Stat.Defence];
        public StatEntity Unknown25 => this[Stat.Unknown25];
        public StatEntity Accuracy => this[Stat.Accuracy];
        public StatEntity Unknown27 => this[Stat.Unknown27];
        public StatEntity PartialDamage => this[Stat.PartialDamage];
        public StatEntity CritChance => this[Stat.CritChance];
        public StatEntity Unknown30 => this[Stat.Unknown30];
        public StatEntity CritResistance => this[Stat.CritResistance];
        public StatEntity Unknown32 => this[Stat.Unknown32];
        public StatEntity Unknown33 => this[Stat.Unknown33];
        public StatEntity Unknown34 => this[Stat.Unknown34];
        public StatEntity CritDamage => this[Stat.CritDamage];
        public StatEntity Unknown36 => this[Stat.Unknown36];
        public StatEntity Unknown37 => this[Stat.Unknown37];
        public StatEntity DamageReduction => this[Stat.DamageReduction];
        public StatEntity Unknown39 => this[Stat.Unknown39];
        public StatEntity Unknown40 => this[Stat.Unknown40];
        public StatEntity Unknown41 => this[Stat.Unknown41];
        public StatEntity Unknown42 => this[Stat.Unknown42];
        public StatEntity Evade => this[Stat.Evade];
        public StatEntity Unknown44 => this[Stat.Unknown44];
        public StatEntity Unknown45 => this[Stat.Unknown45];
        public StatEntity Unknown46 => this[Stat.Unknown46];
        public StatEntity ArmourBreak => this[Stat.ArmourBreak];
        public StatEntity Unknown48 => this[Stat.Unknown48];
        public StatEntity FireResistance => this[Stat.FireResistance];
        public StatEntity PoisonResistance => this[Stat.PoisonResistance];
        public StatEntity ElectricResistance => this[Stat.ElectricResistance];
        public StatEntity BleedResistance => this[Stat.BleedResistance];
        public StatEntity Stun1Resistance => this[Stat.Stun1Resistance];
        public StatEntity ParalysisResistance => this[Stat.ParalysisResistance];
        public StatEntity SleepResistance => this[Stat.SleepResistance];
        public StatEntity FrostResistance => this[Stat.FrostResistance];
        public StatEntity SilenceResistance => this[Stat.SilenceResistance];
        public StatEntity VulnResistance => this[Stat.VulnResistance];
        public StatEntity Stun2Resistance => this[Stat.Stun2Resistance];
        public StatEntity ConfusedResistance => this[Stat.ConfusedResistance];
        public StatEntity Unknown61 => this[Stat.Unknown61];
        public StatEntity Unknown62 => this[Stat.Unknown62];
        public StatEntity VirtueDamage => this[Stat.VirtueDamage];
        public StatEntity SinDamage => this[Stat.SinDamage];
        public StatEntity GraceDamage => this[Stat.GraceDamage];
        public StatEntity HateDamage => this[Stat.HateDamage];
        public StatEntity SolaceDamage => this[Stat.SolaceDamage];
        public StatEntity TormentDamage => this[Stat.TormentDamage];
        public StatEntity VirtueResistance => this[Stat.VirtueResistance];
        public StatEntity SinResistance => this[Stat.SinResistance];
        public StatEntity GraceResistance => this[Stat.GraceResistance];
        public StatEntity HateResistance => this[Stat.HateResistance];
        public StatEntity SolaceResistance => this[Stat.SolaceResistance];
        public StatEntity TormentResistance => this[Stat.TormentResistance];
        public StatEntity ManicBalanceDamage => this[Stat.ManicBalanceDamage];
        public StatEntity ManicBalanceResistance => this[Stat.ManicBalanceResistance];

        public StatsEntity() : base(Enumerable.Range(0, typeof(Stat).GetEnumValues().Cast<byte>().Max()).Select(id => new StatEntity{ Id = (Stat)id, Value = 0.0f }))
        {
        }
    }
}
