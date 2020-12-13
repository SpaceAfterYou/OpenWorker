using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Character
{
    public interface ICharacterStats : IReadOnlyList<ICharacterStat>
    {
        ICharacterStat this[Stat index] => this[(int)index];

        ICharacterStat Unknown0 => this[Stat.Unknown0];
        ICharacterStat CurrentHP => this[Stat.CurrentHP];
        ICharacterStat CurrentSG => this[Stat.CurrentSG];
        ICharacterStat Unknown3 => this[Stat.Unknown3];
        ICharacterStat Unknown4 => this[Stat.Unknown4];
        ICharacterStat Unknown5 => this[Stat.Unknown5];
        ICharacterStat Unknown6 => this[Stat.Unknown6];
        ICharacterStat Unknown7 => this[Stat.Unknown7];
        ICharacterStat Unknown8 => this[Stat.Unknown8];
        ICharacterStat Unknown9 => this[Stat.Unknown9];
        ICharacterStat MaxHP => this[Stat.MaxHP];
        ICharacterStat Unknown11 => this[Stat.Unknown11];
        ICharacterStat MaxSG => this[Stat.MaxSG];
        ICharacterStat Unknown12 => this[Stat.Unknown12];
        ICharacterStat Unknown13 => this[Stat.Unknown13];
        ICharacterStat Stamina => this[Stat.Stamina];
        ICharacterStat StaminaRegeneration => this[Stat.StaminaRegeneration];
        ICharacterStat Unknown16 => this[Stat.Unknown16];
        ICharacterStat Unknown17 => this[Stat.Unknown17];
        ICharacterStat MoveSpeed => this[Stat.MoveSpeed];
        ICharacterStat AttackSpeed => this[Stat.AttackSpeed];
        ICharacterStat MinAttackDamage => this[Stat.MinAttackDamage];
        ICharacterStat MaxAttackDamage => this[Stat.MaxAttackDamage];
        ICharacterStat Unknown22 => this[Stat.Unknown22];
        ICharacterStat Unknown23 => this[Stat.Unknown23];
        ICharacterStat Defence => this[Stat.Defence];
        ICharacterStat Unknown25 => this[Stat.Unknown25];
        ICharacterStat Accuracy => this[Stat.Accuracy];
        ICharacterStat Unknown27 => this[Stat.Unknown27];
        ICharacterStat PartialDamage => this[Stat.PartialDamage];
        ICharacterStat CritChance => this[Stat.CritChance];
        ICharacterStat Unknown30 => this[Stat.Unknown30];
        ICharacterStat CritResistance => this[Stat.CritResistance];
        ICharacterStat Unknown32 => this[Stat.Unknown32];
        ICharacterStat Unknown33 => this[Stat.Unknown33];
        ICharacterStat Unknown34 => this[Stat.Unknown34];
        ICharacterStat CritDamage => this[Stat.CritDamage];
        ICharacterStat Unknown36 => this[Stat.Unknown36];
        ICharacterStat Unknown37 => this[Stat.Unknown37];
        ICharacterStat DamageReduction => this[Stat.DamageReduction];
        ICharacterStat Unknown39 => this[Stat.Unknown39];
        ICharacterStat Unknown40 => this[Stat.Unknown40];
        ICharacterStat Unknown41 => this[Stat.Unknown41];
        ICharacterStat Unknown42 => this[Stat.Unknown42];
        ICharacterStat Evade => this[Stat.Evade];
        ICharacterStat Unknown44 => this[Stat.Unknown44];
        ICharacterStat Unknown45 => this[Stat.Unknown45];
        ICharacterStat Unknown46 => this[Stat.Unknown46];
        ICharacterStat ArmourBreak => this[Stat.ArmourBreak];
        ICharacterStat Unknown48 => this[Stat.Unknown48];
        ICharacterStat FireResistance => this[Stat.FireResistance];
        ICharacterStat PoisonResistance => this[Stat.PoisonResistance];
        ICharacterStat ElectricResistance => this[Stat.ElectricResistance];
        ICharacterStat BleedResistance => this[Stat.BleedResistance];
        ICharacterStat Stun1Resistance => this[Stat.Stun1Resistance];
        ICharacterStat ParalysisResistance => this[Stat.ParalysisResistance];
        ICharacterStat SleepResistance => this[Stat.SleepResistance];
        ICharacterStat FrostResistance => this[Stat.FrostResistance];
        ICharacterStat SilenceResistance => this[Stat.SilenceResistance];
        ICharacterStat VulnResistance => this[Stat.VulnResistance];
        ICharacterStat Stun2Resistance => this[Stat.Stun2Resistance];
        ICharacterStat ConfusedResistance => this[Stat.ConfusedResistance];
        ICharacterStat Unknown61 => this[Stat.Unknown61];
        ICharacterStat Unknown62 => this[Stat.Unknown62];
        ICharacterStat VirtueDamage => this[Stat.VirtueDamage];
        ICharacterStat SinDamage => this[Stat.SinDamage];
        ICharacterStat GraceDamage => this[Stat.GraceDamage];
        ICharacterStat HateDamage => this[Stat.HateDamage];
        ICharacterStat SolaceDamage => this[Stat.SolaceDamage];
        ICharacterStat TormentDamage => this[Stat.TormentDamage];
        ICharacterStat VirtueResistance => this[Stat.VirtueResistance];
        ICharacterStat SinResistance => this[Stat.SinResistance];
        ICharacterStat GraceResistance => this[Stat.GraceResistance];
        ICharacterStat HateResistance => this[Stat.HateResistance];
        ICharacterStat SolaceResistance => this[Stat.SolaceResistance];
        ICharacterStat TormentResistance => this[Stat.TormentResistance];
        ICharacterStat ManicBalanceDamage => this[Stat.ManicBalanceDamage];
        ICharacterStat ManicBalanceResistance => this[Stat.ManicBalanceResistance];
    }
}
