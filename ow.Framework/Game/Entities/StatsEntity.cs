using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game.Entities
{
    public sealed class StatsEntity : List<float>, IStatsEntity
    {
        public float this[Stat index] => this[(int)index];

        public float Unknown0 => this[Stat.Unknown0];
        public float CurrentHp => this[Stat.CurrentHP];
        public float CurrentSg => this[Stat.CurrentSG];
        public float Unknown3 => this[Stat.Unknown3];
        public float Unknown4 => this[Stat.Unknown4];
        public float Unknown5 => this[Stat.Unknown5];
        public float Unknown6 => this[Stat.Unknown6];
        public float Unknown7 => this[Stat.Unknown7];
        public float Unknown8 => this[Stat.Unknown8];
        public float Unknown9 => this[Stat.Unknown9];
        public float MaxHp => this[Stat.MaxHP];
        public float Unknown11 => this[Stat.Unknown11];
        public float MaxSg => this[Stat.MaxSG];
        public float Unknown12 => this[Stat.Unknown12];
        public float Unknown13 => this[Stat.Unknown13];
        public float Stamina => this[Stat.Stamina];
        public float StaminaRegeneration => this[Stat.StaminaRegeneration];
        public float Unknown16 => this[Stat.Unknown16];
        public float Unknown17 => this[Stat.Unknown17];
        public float MoveSpeed => this[Stat.MoveSpeed];
        public float AttackSpeed => this[Stat.AttackSpeed];
        public float MinAttackDamage => this[Stat.MinAttackDamage];
        public float MaxAttackDamage => this[Stat.MaxAttackDamage];
        public float Unknown22 => this[Stat.Unknown22];
        public float Unknown23 => this[Stat.Unknown23];
        public float Defence => this[Stat.Defence];
        public float Unknown25 => this[Stat.Unknown25];
        public float Accuracy => this[Stat.Accuracy];
        public float Unknown27 => this[Stat.Unknown27];
        public float PartialDamage => this[Stat.PartialDamage];
        public float CritChance => this[Stat.CritChance];
        public float Unknown30 => this[Stat.Unknown30];
        public float CritResistance => this[Stat.CritResistance];
        public float Unknown32 => this[Stat.Unknown32];
        public float Unknown33 => this[Stat.Unknown33];
        public float Unknown34 => this[Stat.Unknown34];
        public float CritDamage => this[Stat.CritDamage];
        public float Unknown36 => this[Stat.Unknown36];
        public float Unknown37 => this[Stat.Unknown37];
        public float DamageReduction => this[Stat.DamageReduction];
        public float Unknown39 => this[Stat.Unknown39];
        public float Unknown40 => this[Stat.Unknown40];
        public float Unknown41 => this[Stat.Unknown41];
        public float Unknown42 => this[Stat.Unknown42];
        public float Evade => this[Stat.Evade];
        public float Unknown44 => this[Stat.Unknown44];
        public float Unknown45 => this[Stat.Unknown45];
        public float Unknown46 => this[Stat.Unknown46];
        public float ArmourBreak => this[Stat.ArmourBreak];
        public float Unknown48 => this[Stat.Unknown48];
        public float FireResistance => this[Stat.FireResistance];
        public float PoisonResistance => this[Stat.PoisonResistance];
        public float ElectricResistance => this[Stat.ElectricResistance];
        public float BleedResistance => this[Stat.BleedResistance];
        public float Stun1Resistance => this[Stat.Stun1Resistance];
        public float ParalysisResistance => this[Stat.ParalysisResistance];
        public float SleepResistance => this[Stat.SleepResistance];
        public float FrostResistance => this[Stat.FrostResistance];
        public float SilenceResistance => this[Stat.SilenceResistance];
        public float VulnResistance => this[Stat.VulnResistance];
        public float Stun2Resistance => this[Stat.Stun2Resistance];
        public float ConfusedResistance => this[Stat.ConfusedResistance];
        public float Unknown61 => this[Stat.Unknown61];
        public float Unknown62 => this[Stat.Unknown62];
        public float VirtueDamage => this[Stat.VirtueDamage];
        public float SinDamage => this[Stat.SinDamage];
        public float GraceDamage => this[Stat.GraceDamage];
        public float HateDamage => this[Stat.HateDamage];
        public float SolaceDamage => this[Stat.SolaceDamage];
        public float TormentDamage => this[Stat.TormentDamage];
        public float VirtueResistance => this[Stat.VirtueResistance];
        public float SinResistance => this[Stat.SinResistance];
        public float GraceResistance => this[Stat.GraceResistance];
        public float HateResistance => this[Stat.HateResistance];
        public float SolaceResistance => this[Stat.SolaceResistance];
        public float TormentResistance => this[Stat.TormentResistance];
        public float ManicBalanceDamage => this[Stat.ManicBalanceDamage];
        public float ManicBalanceResistance => this[Stat.ManicBalanceResistance];

        public StatsEntity() : base(Enumerable.Range(0, typeof(Stat).GetEnumValues().Cast<byte>().Max()).Select(id => 0.0f))
        {
        }
    }
}