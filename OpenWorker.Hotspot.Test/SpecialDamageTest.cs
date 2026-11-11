using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The special effects of gear that raise the damage to a kind of monster and lower the damage a kind of monster does.
/// </summary>
[TestClass]
public sealed class SpecialDamageTest
{
    private static readonly Opponent Plain = new(1, 3, 0);
    private static readonly Opponent Boss = new(4, 6, 0);

    private static CombatStats Wearing(params (int Effect, float Value)[] effects)
    {
        var sheet = new StatSheet();

        foreach (var (effect, value) in effects)
        {
            sheet.Apply(effect, value);
        }

        return new CombatStats { Special = sheet.Special };
    }

    /// <summary>
    /// A special effect written to the sheet is kept by its number, and a sheet that has none has the same effects as any other.
    /// </summary>
    [TestMethod]
    public void TheSheetKeepsTheSpecialEffectsItIsGiven()
    {
        var stats = Wearing((104, 12.5f), (104, 7.5f), (126, 5f));

        Assert.AreEqual(20f, stats.Special.Of(SpecialEffect.DamageToBoss));
        Assert.AreEqual(5f, stats.Special.DamageToFaction(6), "the first faction has the first of the effects of a faction");
        Assert.AreEqual(0f, stats.Special.DamageToFaction(7));
        Assert.AreEqual(new CombatStats(), Wearing(), "nothing worn is the same as nothing");
        Assert.AreEqual(Wearing((104, 20f)).Special, Wearing((104, 5f), (104, 15f)).Special, "the same numbers are the same effects");
    }

    /// <summary>
    /// The damage a player does to a monster grows by the share for its kind, each share taking its part of what the one before
    /// it left: a boss counts for the boss share and not for the plain one, a faction has its own.
    /// </summary>
    [TestMethod]
    public void AMonsterTakesMoreDamageByItsKind()
    {
        var player = Wearing((103, 50f), (104, 100f), (105, 25f), (107, 100f), (126, 50f));
        var nobody = new CombatStats();

        Assert.AreEqual(187, SpecialDamage.Apply(100, player, nobody, DamageContext.PlayerHits(Plain, false, false), false), "a plain monster of a faction without an effect");
        Assert.AreEqual(375, SpecialDamage.Apply(100, player, nobody, DamageContext.PlayerHits(Boss, false, false), false), "a boss of the first faction");
        Assert.AreEqual(374, SpecialDamage.Apply(100, player, nobody, DamageContext.PlayerHits(Plain, true, false), false), "in the air the share for flying counts");
    }

    /// <summary>
    /// A player who wears effects against a kind of monster takes less from it, and less still from a blow that only grazed.
    /// </summary>
    [TestMethod]
    public void APlayerTakesLessDamageFromTheKindItWearsEffectsAgainst()
    {
        var player = Wearing((109, 50f), (111, 25f), (142, 50f));
        var nobody = new CombatStats();

        Assert.AreEqual(38, SpecialDamage.Apply(100, nobody, player, DamageContext.MonsterHits(Plain), false));
        Assert.AreEqual(19, SpecialDamage.Apply(100, nobody, player, DamageContext.MonsterHits(Plain), true), "a graze loses half of the rest");
        Assert.AreEqual(100, SpecialDamage.Apply(100, nobody, player, default, false), "no monster in the hit");
    }
}
