using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Character
{
    public sealed class StatsCharacter : List<StatCharacter>
    {
        public StatCharacter this[Stat index] => this[(int)index];

        public StatCharacter Unknown0 => this[Stat.Unknown0];
        public StatCharacter CurrentHP => this[Stat.CurrentHP];
        public StatCharacter CurrentSG => this[Stat.CurrentSG];
        public StatCharacter Unknown3 => this[Stat.Unknown3];
        public StatCharacter Unknown4 => this[Stat.Unknown4];
        public StatCharacter Unknown5 => this[Stat.Unknown5];
        public StatCharacter Unknown6 => this[Stat.Unknown6];
        public StatCharacter Unknown7 => this[Stat.Unknown7];
        public StatCharacter Unknown8 => this[Stat.Unknown8];
        public StatCharacter Unknown9 => this[Stat.Unknown9];
        public StatCharacter MaxHP => this[Stat.MaxHP];
        public StatCharacter Unknown11 => this[Stat.Unknown11];
        public StatCharacter MaxSG => this[Stat.MaxSG];
        public StatCharacter Unknown12 => this[Stat.Unknown12];
        public StatCharacter Unknown13 => this[Stat.Unknown13];
        public StatCharacter Stamina => this[Stat.Stamina];
        public StatCharacter StaminaRegeneration => this[Stat.StaminaRegeneration];
        public StatCharacter Unknown16 => this[Stat.Unknown16];
        public StatCharacter Unknown17 => this[Stat.Unknown17];
        public StatCharacter MoveSpeed => this[Stat.MoveSpeed];
        public StatCharacter AttackSpeed => this[Stat.AttackSpeed];
        public StatCharacter MinAttackDamage => this[Stat.MinAttackDamage];
        public StatCharacter MaxAttackDamage => this[Stat.MaxAttackDamage];
        public StatCharacter Unknown22 => this[Stat.Unknown22];
        public StatCharacter Unknown23 => this[Stat.Unknown23];
        public StatCharacter Defence => this[Stat.Defence];
        public StatCharacter Unknown25 => this[Stat.Unknown25];
        public StatCharacter Accuracy => this[Stat.Accuracy];
        public StatCharacter Unknown27 => this[Stat.Unknown27];
        public StatCharacter PartialDamage => this[Stat.PartialDamage];
        public StatCharacter CritChance => this[Stat.CritChance];
        public StatCharacter Unknown30 => this[Stat.Unknown30];
        public StatCharacter CritResistance => this[Stat.CritResistance];
        public StatCharacter Unknown32 => this[Stat.Unknown32];
        public StatCharacter Unknown33 => this[Stat.Unknown33];
        public StatCharacter Unknown34 => this[Stat.Unknown34];
        public StatCharacter CritDamage => this[Stat.CritDamage];
        public StatCharacter Unknown36 => this[Stat.Unknown36];
        public StatCharacter Unknown37 => this[Stat.Unknown37];
        public StatCharacter DamageReduction => this[Stat.DamageReduction];
        public StatCharacter Unknown39 => this[Stat.Unknown39];
        public StatCharacter Unknown40 => this[Stat.Unknown40];
        public StatCharacter Unknown41 => this[Stat.Unknown41];
        public StatCharacter Unknown42 => this[Stat.Unknown42];
        public StatCharacter Evade => this[Stat.Evade];
        public StatCharacter Unknown44 => this[Stat.Unknown44];
        public StatCharacter Unknown45 => this[Stat.Unknown45];
        public StatCharacter Unknown46 => this[Stat.Unknown46];
        public StatCharacter ArmourBreak => this[Stat.ArmourBreak];
        public StatCharacter Unknown48 => this[Stat.Unknown48];
        public StatCharacter FireResistance => this[Stat.FireResistance];
        public StatCharacter PoisonResistance => this[Stat.PoisonResistance];
        public StatCharacter ElectricResistance => this[Stat.ElectricResistance];
        public StatCharacter BleedResistance => this[Stat.BleedResistance];
        public StatCharacter Stun1Resistance => this[Stat.Stun1Resistance];
        public StatCharacter ParalysisResistance => this[Stat.ParalysisResistance];
        public StatCharacter SleepResistance => this[Stat.SleepResistance];
        public StatCharacter FrostResistance => this[Stat.FrostResistance];
        public StatCharacter SilenceResistance => this[Stat.SilenceResistance];
        public StatCharacter VulnResistance => this[Stat.VulnResistance];
        public StatCharacter Stun2Resistance => this[Stat.Stun2Resistance];
        public StatCharacter ConfusedResistance => this[Stat.ConfusedResistance];
        public StatCharacter Unknown61 => this[Stat.Unknown61];
        public StatCharacter Unknown62 => this[Stat.Unknown62];
        public StatCharacter VirtueDamage => this[Stat.VirtueDamage];
        public StatCharacter SinDamage => this[Stat.SinDamage];
        public StatCharacter GraceDamage => this[Stat.GraceDamage];
        public StatCharacter HateDamage => this[Stat.HateDamage];
        public StatCharacter SolaceDamage => this[Stat.SolaceDamage];
        public StatCharacter TormentDamage => this[Stat.TormentDamage];
        public StatCharacter VirtueResistance => this[Stat.VirtueResistance];
        public StatCharacter SinResistance => this[Stat.SinResistance];
        public StatCharacter GraceResistance => this[Stat.GraceResistance];
        public StatCharacter HateResistance => this[Stat.HateResistance];
        public StatCharacter SolaceResistance => this[Stat.SolaceResistance];
        public StatCharacter TormentResistance => this[Stat.TormentResistance];
        public StatCharacter ManicBalanceDamage => this[Stat.ManicBalanceDamage];
        public StatCharacter ManicBalanceResistance => this[Stat.ManicBalanceResistance];
    }
}