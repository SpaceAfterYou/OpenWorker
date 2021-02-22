using SoulCore.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class Stats : List<Stats.Stat>
    {
        public sealed class Stat
        {
            internal CharacterStat Id { get; init; }
            internal float Value { get; set; }
        }

        internal Stat this[CharacterStat index] => this[(int)index];

        internal Stat Unknown0 => this[CharacterStat.Unknown0];
        internal Stat CurrentHp => this[CharacterStat.CurrentHp];
        internal Stat CurrentSg => this[CharacterStat.CurrentSg];
        internal Stat Unknown3 => this[CharacterStat.Unknown3];
        internal Stat Unknown4 => this[CharacterStat.Unknown4];
        internal Stat Unknown5 => this[CharacterStat.Unknown5];
        internal Stat Unknown6 => this[CharacterStat.Unknown6];
        internal Stat Unknown7 => this[CharacterStat.Unknown7];
        internal Stat Unknown8 => this[CharacterStat.Unknown8];
        internal Stat Unknown9 => this[CharacterStat.Unknown9];
        internal Stat MaxHp => this[CharacterStat.MaxHp];
        internal Stat Unknown11 => this[CharacterStat.Unknown11];
        internal Stat MaxSg => this[CharacterStat.MaxSg];
        internal Stat Unknown12 => this[CharacterStat.Unknown12];
        internal Stat Unknown13 => this[CharacterStat.Unknown13];
        internal Stat Stamina => this[CharacterStat.Stamina];
        internal Stat StaminaRegeneration => this[CharacterStat.StaminaRegeneration];
        internal Stat Unknown16 => this[CharacterStat.Unknown16];
        internal Stat Unknown17 => this[CharacterStat.Unknown17];
        internal Stat MoveSpeed => this[CharacterStat.MoveSpeed];
        internal Stat AttackSpeed => this[CharacterStat.AttackSpeed];
        internal Stat MinAttackDamage => this[CharacterStat.MinAttackDamage];
        internal Stat MaxAttackDamage => this[CharacterStat.MaxAttackDamage];
        internal Stat Unknown22 => this[CharacterStat.Unknown22];
        internal Stat Unknown23 => this[CharacterStat.Unknown23];
        internal Stat Defence => this[CharacterStat.Defence];
        internal Stat Unknown25 => this[CharacterStat.Unknown25];
        internal Stat Accuracy => this[CharacterStat.Accuracy];
        internal Stat Unknown27 => this[CharacterStat.Unknown27];
        internal Stat PartialDamage => this[CharacterStat.PartialDamage];
        internal Stat CritChance => this[CharacterStat.CritChance];
        internal Stat Unknown30 => this[CharacterStat.Unknown30];
        internal Stat CritResistance => this[CharacterStat.CritResistance];
        internal Stat Unknown32 => this[CharacterStat.Unknown32];
        internal Stat Unknown33 => this[CharacterStat.Unknown33];
        internal Stat Unknown34 => this[CharacterStat.Unknown34];
        internal Stat CritDamage => this[CharacterStat.CritDamage];
        internal Stat Unknown36 => this[CharacterStat.Unknown36];
        internal Stat Unknown37 => this[CharacterStat.Unknown37];
        internal Stat DamageReduction => this[CharacterStat.DamageReduction];
        internal Stat Unknown39 => this[CharacterStat.Unknown39];
        internal Stat Unknown40 => this[CharacterStat.Unknown40];
        internal Stat Unknown41 => this[CharacterStat.Unknown41];
        internal Stat Unknown42 => this[CharacterStat.Unknown42];
        internal Stat Evade => this[CharacterStat.Evade];
        internal Stat Unknown44 => this[CharacterStat.Unknown44];
        internal Stat Unknown45 => this[CharacterStat.Unknown45];
        internal Stat Unknown46 => this[CharacterStat.Unknown46];
        internal Stat ArmourBreak => this[CharacterStat.ArmourBreak];
        internal Stat Unknown48 => this[CharacterStat.Unknown48];
        internal Stat FireResistance => this[CharacterStat.FireResistance];
        internal Stat PoisonResistance => this[CharacterStat.PoisonResistance];
        internal Stat ElectricResistance => this[CharacterStat.ElectricResistance];
        internal Stat BleedResistance => this[CharacterStat.BleedResistance];
        internal Stat Stun1Resistance => this[CharacterStat.Stun1Resistance];
        internal Stat ParalysisResistance => this[CharacterStat.ParalysisResistance];
        internal Stat SleepResistance => this[CharacterStat.SleepResistance];
        internal Stat FrostResistance => this[CharacterStat.FrostResistance];
        internal Stat SilenceResistance => this[CharacterStat.SilenceResistance];
        internal Stat VulnResistance => this[CharacterStat.VulnResistance];
        internal Stat Stun2Resistance => this[CharacterStat.Stun2Resistance];
        internal Stat ConfusedResistance => this[CharacterStat.ConfusedResistance];
        internal Stat Unknown61 => this[CharacterStat.Unknown61];
        internal Stat Unknown62 => this[CharacterStat.Unknown62];
        internal Stat VirtueDamage => this[CharacterStat.VirtueDamage];
        internal Stat SinDamage => this[CharacterStat.SinDamage];
        internal Stat GraceDamage => this[CharacterStat.GraceDamage];
        internal Stat HateDamage => this[CharacterStat.HateDamage];
        internal Stat SolaceDamage => this[CharacterStat.SolaceDamage];
        internal Stat TormentDamage => this[CharacterStat.TormentDamage];
        internal Stat VirtueResistance => this[CharacterStat.VirtueResistance];
        internal Stat SinResistance => this[CharacterStat.SinResistance];
        internal Stat GraceResistance => this[CharacterStat.GraceResistance];
        internal Stat HateResistance => this[CharacterStat.HateResistance];
        internal Stat SolaceResistance => this[CharacterStat.SolaceResistance];
        internal Stat TormentResistance => this[CharacterStat.TormentResistance];
        internal Stat ManicBalanceDamage => this[CharacterStat.ManicBalanceDamage];
        internal Stat ManicBalanceResistance => this[CharacterStat.ManicBalanceResistance];

        internal Stats() : base(Enumerable.Range(0, typeof(CharacterStat).GetEnumValues().Cast<ushort>().Max() + 1).Select(id => new Stat { Id = (CharacterStat)id, Value = 100.0f }))
        {
            Debug.Assert(this.Last().Id == CharacterStat.ManicBalanceResistance);
        }
    }
}