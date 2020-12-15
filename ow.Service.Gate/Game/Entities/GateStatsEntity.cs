using ow.Framework.Game.Entities;
using ow.Framework.Game.Enums;
using System.Collections;
using System.Collections.Generic;

namespace ow.Service.Gate.Game.Entities
{
    internal class GateStatsEntity : IStatsEntity
    {
        public StatEntity this[int index] => throw new System.NotImplementedException();

        public StatEntity Unknown0 => new() { Id = Stat.Unknown0, Value = 0.0f };
        public StatEntity CurrentHp => new() { Id = Stat.CurrentHp, Value = 0.0f };
        public StatEntity CurrentSg => new() { Id = Stat.CurrentSg, Value = 0.0f };
        public StatEntity Unknown3 => new() { Id = Stat.Unknown3, Value = 0.0f };
        public StatEntity Unknown4 => new() { Id = Stat.Unknown4, Value = 0.0f };
        public StatEntity Unknown5 => new() { Id = Stat.Unknown5, Value = 0.0f };
        public StatEntity Unknown6 => new() { Id = Stat.Unknown6, Value = 0.0f };
        public StatEntity Unknown7 => new() { Id = Stat.Unknown7, Value = 0.0f };
        public StatEntity Unknown8 => new() { Id = Stat.Unknown8, Value = 0.0f };
        public StatEntity Unknown9 => new() { Id = Stat.Unknown9, Value = 0.0f };
        public StatEntity MaxHp => new() { Id = Stat.MaxHp, Value = 0.0f };
        public StatEntity Unknown11 => new() { Id = Stat.Unknown11, Value = 0.0f };
        public StatEntity MaxSg => new() { Id = Stat.MaxSg, Value = 0.0f };
        public StatEntity Unknown12 => new() { Id = Stat.Unknown12, Value = 0.0f };
        public StatEntity Unknown13 => new() { Id = Stat.Unknown13, Value = 0.0f };
        public StatEntity Stamina => new() { Id = Stat.Stamina, Value = 0.0f };
        public StatEntity StaminaRegeneration => new() { Id = Stat.StaminaRegeneration, Value = 0.0f };
        public StatEntity Unknown16 => new() { Id = Stat.Unknown16, Value = 0.0f };
        public StatEntity Unknown17 => new() { Id = Stat.Unknown17, Value = 0.0f };
        public StatEntity MoveSpeed => new() { Id = Stat.MoveSpeed, Value = 1.0f };
        public StatEntity AttackSpeed => new() { Id = Stat.AttackSpeed, Value = 1.0f };
        public StatEntity MinAttackDamage => new() { Id = Stat.MinAttackDamage, Value = 0.0f };
        public StatEntity MaxAttackDamage => new() { Id = Stat.MaxAttackDamage, Value = 0.0f };
        public StatEntity Unknown22 => new() { Id = Stat.Unknown22, Value = 0.0f };
        public StatEntity Unknown23 => new() { Id = Stat.Unknown23, Value = 0.0f };
        public StatEntity Defence => new() { Id = Stat.Defence, Value = 0.0f };
        public StatEntity Unknown25 => new() { Id = Stat.Unknown25, Value = 0.0f };
        public StatEntity Accuracy => new() { Id = Stat.Accuracy, Value = 0.0f };
        public StatEntity Unknown27 => new() { Id = Stat.Unknown27, Value = 0.0f };
        public StatEntity PartialDamage => new() { Id = Stat.PartialDamage, Value = 0.0f };
        public StatEntity CritChance => new() { Id = Stat.CritChance, Value = 0.0f };
        public StatEntity Unknown30 => new() { Id = Stat.Unknown30, Value = 0.0f };
        public StatEntity CritResistance => new() { Id = Stat.CritResistance, Value = 0.0f };
        public StatEntity Unknown32 => new() { Id = Stat.Unknown32, Value = 0.0f };
        public StatEntity Unknown33 => new() { Id = Stat.Unknown33, Value = 0.0f };
        public StatEntity Unknown34 => new() { Id = Stat.Unknown34, Value = 0.0f };
        public StatEntity CritDamage => new() { Id = Stat.CritDamage, Value = 0.0f };
        public StatEntity Unknown36 => new() { Id = Stat.Unknown36, Value = 0.0f };
        public StatEntity Unknown37 => new() { Id = Stat.Unknown37, Value = 0.0f };
        public StatEntity DamageReduction => new() { Id = Stat.DamageReduction, Value = 0.0f };
        public StatEntity Unknown39 => new() { Id = Stat.Unknown39, Value = 0.0f };
        public StatEntity Unknown40 => new() { Id = Stat.Unknown40, Value = 0.0f };
        public StatEntity Unknown41 => new() { Id = Stat.Unknown41, Value = 0.0f };
        public StatEntity Unknown42 => new() { Id = Stat.Unknown42, Value = 0.0f };
        public StatEntity Evade => new() { Id = Stat.Evade, Value = 0.0f };
        public StatEntity Unknown44 => new() { Id = Stat.Unknown44, Value = 0.0f };
        public StatEntity Unknown45 => new() { Id = Stat.Unknown45, Value = 0.0f };
        public StatEntity Unknown46 => new() { Id = Stat.Unknown46, Value = 0.0f };
        public StatEntity ArmourBreak => new() { Id = Stat.ArmourBreak, Value = 0.0f };
        public StatEntity Unknown48 => new() { Id = Stat.Unknown48, Value = 0.0f };
        public StatEntity FireResistance => new() { Id = Stat.FireResistance, Value = 0.0f };
        public StatEntity PoisonResistance => new() { Id = Stat.PoisonResistance, Value = 0.0f };
        public StatEntity ElectricResistance => new() { Id = Stat.ElectricResistance, Value = 0.0f };
        public StatEntity BleedResistance => new() { Id = Stat.BleedResistance, Value = 0.0f };
        public StatEntity Stun1Resistance => new() { Id = Stat.Stun1Resistance, Value = 0.0f };
        public StatEntity ParalysisResistance => new() { Id = Stat.ParalysisResistance, Value = 0.0f };
        public StatEntity SleepResistance => new() { Id = Stat.SleepResistance, Value = 0.0f };
        public StatEntity FrostResistance => new() { Id = Stat.FrostResistance, Value = 0.0f };
        public StatEntity SilenceResistance => new() { Id = Stat.SilenceResistance, Value = 0.0f };
        public StatEntity VulnResistance => new() { Id = Stat.VulnResistance, Value = 0.0f };
        public StatEntity Stun2Resistance => new() { Id = Stat.Stun2Resistance, Value = 0.0f };
        public StatEntity ConfusedResistance => new() { Id = Stat.ConfusedResistance, Value = 0.0f };
        public StatEntity Unknown61 => new() { Id = Stat.Unknown61, Value = 0.0f };
        public StatEntity Unknown62 => new() { Id = Stat.Unknown62, Value = 0.0f };
        public StatEntity VirtueDamage => new() { Id = Stat.VirtueDamage, Value = 0.0f };
        public StatEntity SinDamage => new() { Id = Stat.SinDamage, Value = 0.0f };
        public StatEntity GraceDamage => new() { Id = Stat.GraceDamage, Value = 0.0f };
        public StatEntity HateDamage => new() { Id = Stat.HateDamage, Value = 0.0f };
        public StatEntity SolaceDamage => new() { Id = Stat.SolaceDamage, Value = 0.0f };
        public StatEntity TormentDamage => new() { Id = Stat.TormentDamage, Value = 0.0f };
        public StatEntity VirtueResistance => new() { Id = Stat.VirtueResistance, Value = 0.0f };
        public StatEntity SinResistance => new() { Id = Stat.SinResistance, Value = 0.0f };
        public StatEntity GraceResistance => new() { Id = Stat.GraceResistance, Value = 0.0f };
        public StatEntity HateResistance => new() { Id = Stat.HateResistance, Value = 0.0f };
        public StatEntity SolaceResistance => new() { Id = Stat.SolaceResistance, Value = 0.0f };
        public StatEntity TormentResistance => new() { Id = Stat.TormentResistance, Value = 0.0f };
        public StatEntity ManicBalanceDamage => new() { Id = Stat.ManicBalanceDamage, Value = 0.0f };
        public StatEntity ManicBalanceResistance => new() { Id = Stat.ManicBalanceResistance, Value = 0.0f };

        public int Count => throw new System.NotImplementedException();

        public IEnumerator<StatEntity> GetEnumerator() => throw new System.NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
    }
}