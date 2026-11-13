using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Titles: what the table says of them, what they do to a character, and the lists the client is given.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TitleTest
{
    private static TitleInfoRow Row(int id, TitleKind kind, byte hero, params (short Type, float Value)[] effects)
    {
        return Row(id, kind, hero, 2, 99, effects);
    }

    private static TitleInfoRow Row(int id, TitleKind kind, byte hero, byte condition, int value, params (short Type, float Value)[] effects)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(id);
        writer.Write(id);
        writer.Write((byte)kind);
        writer.Write(hero);
        writer.Write((byte)1);
        writer.Write(condition);
        writer.Write(value);
        writer.Write(new byte[6]);

        for (var slot = 0; slot < 5; slot++)
        {
            writer.Write(slot < effects.Length ? effects[slot].Type : (short)0);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            writer.Write(slot < effects.Length ? effects[slot].Value : 0f);
        }

        writer.Flush();
        stream.Position = 0;

        return new TitleInfoRow(new BinaryReader(stream));
    }

    private static TitleCatalog Catalog(params TitleInfoRow[] rows)
    {
        return new TitleCatalog(new ReadOnlyCollection<TitleInfoRow>(rows));
    }

    private static ReadOnlyCollection<T> Empty<T>()
    {
        return new ReadOnlyCollection<T>([]);
    }

    private static CombatTables Tables(TitleCatalog? catalog)
    {
        return new CombatTables(Empty<MonsterRow>(), Empty<StatusRow>(), Empty<SkillRow>(), Empty<CharacterInfoRow>(), catalog);
    }

    private static byte[] Written(PersonLoadTitleResponse message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void ATitleKnowsItsKindItsClassAndItsEffects()
    {
        var catalog = Catalog(
            Row(100, TitleKind.Prefix, 0, (StatEffects.HealthShare, 2.25f)),
            Row(200, TitleKind.Suffix, 2, (StatEffects.StrengthNumber, 10f), (StatEffects.LuckNumber, 3f)),
            Row(300, TitleKind.Prefix, 0));

        Assert.IsTrue(catalog.TryGet(100, out var prefix));
        Assert.AreEqual(TitleKind.Prefix, prefix.Kind);
        Assert.IsTrue(prefix.CanBeUsedBy(Hero.Haru));
        Assert.IsTrue(prefix.CanBeUsedBy(Hero.Iris));
        CollectionAssert.AreEqual(new[] { new StatEffect(StatEffects.HealthShare, 2.25f) }, prefix.Effects);

        Assert.IsTrue(catalog.TryGet(200, out var suffix));
        Assert.AreEqual(TitleKind.Suffix, suffix.Kind);
        Assert.IsFalse(suffix.CanBeUsedBy(Hero.Haru));
        Assert.IsTrue(suffix.CanBeUsedBy(Hero.Erwin));
        Assert.HasCount(2, suffix.Effects);

        Assert.IsTrue(catalog.TryGet(300, out var plain));
        Assert.IsEmpty(plain.Effects);

        Assert.IsFalse(catalog.TryGet(999, out _));
    }

    [TestMethod]
    public void AnEffectThatIsNotOnTheCharacteristicsIsLeftOut()
    {
        // The table is read with the group of the effect in a column of its own: only the group zero is about characteristics.
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(500);
        writer.Write(500);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)1);
        writer.Write((byte)2);
        writer.Write(99);
        writer.Write((byte)0);
        writer.Write(new byte[] { 1, 0, 0, 0, 0 });

        foreach (var type in new short[] { StatEffects.StrengthNumber, StatEffects.AgilityNumber, 0, 0, 0 })
        {
            writer.Write(type);
        }

        foreach (var value in new[] { 5f, 6f, 0f, 0f, 0f })
        {
            writer.Write(value);
        }

        writer.Flush();
        stream.Position = 0;

        var catalog = Catalog(new TitleInfoRow(new BinaryReader(stream)));

        Assert.IsTrue(catalog.TryGet(500, out var title));
        CollectionAssert.AreEqual(new[] { new StatEffect(StatEffects.AgilityNumber, 6f) }, title.Effects);
    }

    [TestMethod]
    public void ANumberIsAddedBeforeAShareIsTaken()
    {
        var stats = new CombatStats { Strength = 100, MaxHealth = 1000, Luck = 7 };

        var result = StatEffects.Apply(stats,
        [
            new StatEffect(StatEffects.StrengthNumber, 10f),
            new StatEffect(StatEffects.StrengthShare, 10f),
            new StatEffect(StatEffects.HealthShare, 5f)
        ]);

        Assert.AreEqual(121f, result.Strength, 0.001f);
        Assert.AreEqual(1050f, result.MaxHealth, 0.001f);
        Assert.AreEqual(7f, result.Luck);
    }

    [TestMethod]
    public void EffectsReachTheDamageStatsOfBothKinds()
    {
        var stats = new CombatStats
        {
            Physical = new DamageStats { AttackMax = 200, Defense = 50, CriticalChance = 3 },
            Magical = new DamageStats { AttackMax = 100, CriticalDamage = 150 }
        };

        var result = StatEffects.Apply(stats,
        [
            new StatEffect(StatEffects.PhysicalAttackShare, 5.25f),
            new StatEffect(StatEffects.PhysicalDefenseNumber, 20f),
            new StatEffect(StatEffects.PhysicalCriticalChanceNumber, 2f),
            new StatEffect(StatEffects.MagicalCriticalDamageNumber, 10f)
        ]);

        Assert.AreEqual(210.5f, result.Physical.AttackMax, 0.001f);
        Assert.AreEqual(70f, result.Physical.Defense);
        Assert.AreEqual(5f, result.Physical.CriticalChance);
        Assert.AreEqual(160f, result.Magical.CriticalDamage);
        Assert.AreEqual(100f, result.Magical.AttackMax);
    }

    [TestMethod]
    public void NothingChangesWithoutEffects()
    {
        var stats = new CombatStats { Strength = 12, MaxHealth = 345 };

        Assert.AreEqual(stats, StatEffects.Apply(stats, []));
        Assert.AreEqual(stats, StatEffects.Apply(stats, [new StatEffect(StatEffects.LuckNumber, 0f)]));
    }

    [TestMethod]
    public void OnlyTheTitlesOfTheAbilitiesChangeWhatACharacterCanDo()
    {
        var catalog = Catalog(
            Row(100, TitleKind.Prefix, 0, (StatEffects.StrengthNumber, 10f)),
            Row(200, TitleKind.Suffix, 0, (StatEffects.StrengthNumber, 5f)),
            Row(300, TitleKind.Prefix, 2, (StatEffects.StrengthNumber, 1000f)));

        var tables = Tables(catalog);

        var both = tables.GetPerson(Hero.Haru, 25, new TitleComponent { AbilityPrimary = 100, AbilitySecondary = 200, Primary = 0, Secondary = 0 });
        var shown = tables.GetPerson(Hero.Haru, 25, new TitleComponent { Primary = 100, Secondary = 200 });
        var other = tables.GetPerson(Hero.Haru, 25, new TitleComponent { AbilityPrimary = 300, Primary = 0, Secondary = 0 });
        var none = tables.GetPerson(Hero.Haru, 25, new TitleComponent { Primary = 0, Secondary = 0 });

        Assert.AreEqual(15f, both.Strength);
        Assert.AreEqual(0f, shown.Strength);
        Assert.AreEqual(0f, other.Strength, "the title of another class does nothing");
        Assert.AreEqual(0f, none.Strength);
    }

    [TestMethod]
    public void WithoutATableOfTitlesTheyDoNothing()
    {
        var tables = Tables(null);

        var result = tables.GetPerson(Hero.Haru, 25, new TitleComponent { AbilityPrimary = 100, AbilitySecondary = 200, Primary = 0, Secondary = 0 });

        Assert.AreEqual(tables.GetPerson(Hero.Haru, 25), result);
    }

    [TestMethod]
    public void ATitleOpensForTheAchievementOrTheLevelItsRowNamesAndOnlyTheFirstOfEachKindForAHeroThatCanUseIt()
    {
        const int achievement = 4000101;
        var catalog = Catalog(
            Row(10, TitleKind.Prefix, 0, OpenTitleManager.ByAchievement, achievement),
            Row(11, TitleKind.Prefix, 0, OpenTitleManager.ByAchievement, achievement),
            Row(20, TitleKind.Suffix, 0, OpenTitleManager.ByAchievement, achievement),
            Row(30, TitleKind.Prefix, (byte)Hero.Haru, OpenTitleManager.ByLevel, 10),
            Row(31, TitleKind.Prefix, (byte)Hero.Jin, OpenTitleManager.ByLevel, 10));

        CollectionAssert.AreEqual(new[] { 10, 20 }, catalog.OpenedBy(OpenTitleManager.ByAchievement, achievement, Hero.Haru).ToArray(), "a prefix and a suffix, the first of each");
        CollectionAssert.AreEqual(new[] { 30 }, catalog.OpenedBy(OpenTitleManager.ByLevel, 10, Hero.Haru).ToArray());
        CollectionAssert.AreEqual(new[] { 31 }, catalog.OpenedBy(OpenTitleManager.ByLevel, 10, Hero.Jin).ToArray(), "the title of another hero");
        Assert.IsEmpty(catalog.OpenedBy(OpenTitleManager.ByLevel, 11, Hero.Haru).ToArray());
        Assert.IsEmpty(catalog.OpenedBy(OpenTitleManager.ByAchievement, 10, Hero.Haru).ToArray(), "the level is not an achievement");
    }

    [TestMethod]
    public async Task AnAchievementReachedAndALevelReachedOpenTheirTitlesWhichAreToldOnceAndNotListedOnceEarned()
    {
        using var world = World.Create();

        var catalog = Catalog(
            Row(10, TitleKind.Prefix, 0, OpenTitleManager.ByAchievement, AchievementChains.FirstAchievement),
            Row(20, TitleKind.Suffix, 0, OpenTitleManager.ByAchievement, AchievementChains.FirstAchievement),
            Row(30, TitleKind.Prefix, 0, OpenTitleManager.ByLevel, 10));
        var titles = new OpenTitleManager(world, new PersonStateStore(null!), catalog);
        var chains = new AchievementChains(world, 2, titles, (AchievementType.Death, 0));
        var connection = new RecordingConnection();
        var dirty = new PersonDirty(7);

        var player = world.Create(
            Component.GetComponentType(typeof(AchievementComponent)),
            Component.GetComponentType(typeof(OpenTitleComponent)),
            Component.GetComponentType(typeof(PersonInfoComponent)),
            Component.GetComponentType(typeof(TitleComponent)),
            Component.GetComponentType(typeof(DirtyComponent)),
            Component.GetComponentType(typeof(ServerSessionComponent)));

        world.Set(player, new OpenTitleComponent { Person = 7, Book = new OpenTitleBook() });
        world.Set(player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
        world.Set(player, new TitleComponent { Primary = 0, Secondary = 0 });
        world.Set(player, new DirtyComponent(dirty));
        world.Set(player, new ServerSessionComponent(connection));
        chains.Enter(player);

        chains.Manager.Count(player, AchievementType.Death);

        Assert.IsEmpty(titles.OpenOf(player), "one death of two reaches nothing");

        chains.Manager.Count(player, AchievementType.Death);

        CollectionAssert.AreEqual(new[] { 10, 20 }, titles.OpenOf(player).ToArray(), "the achievement reached opens its prefix and its suffix");
        Assert.IsTrue(dirty.Sections.HasFlag(PersonSections.Progress), "the flush writes the open titles");

        chains.Manager.OnLevelReached(player, 9);

        Assert.HasCount(2, titles.OpenOf(player));

        chains.Manager.OnLevelReached(player, 10);
        chains.Manager.OnLevelReached(player, 10);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, titles.OpenOf(player).ToArray(), "the level opens its title, once");

        // The client is told each title that opens, once.
        await MetricAssertions.UntilAsync(
            () => connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.Character, (byte)CharacterOpcode.UpdateOpenTitle)) >= 3,
            "The player is told the three titles.");

        var told = connection.Frames.Select(PlayerFrame.Open)
            .Where(e => e.Is(GroupOpcode.Character, (byte)CharacterOpcode.UpdateOpenTitle))
            .Select(e => BinaryPrimitives.ReadInt32LittleEndian(e.Body))
            .ToArray();

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, told);

        // A title the person earns is no longer one that is open.
        world.Set(player, new TitleComponent { Primary = 0, Secondary = 0, Owned = [20] });

        CollectionAssert.AreEqual(new[] { 10, 30 }, titles.OpenOf(player).ToArray());

        var messages = TitleBook.Messages([20], titles.OpenOf(player)).ToArray();

        Assert.HasCount(1, messages);
        CollectionAssert.AreEqual(new[] { 20 }, messages[0].TitleList.ToArray());
        CollectionAssert.AreEqual(new[] { 10, 30 }, messages[0].OpenList.ToArray());
    }

    [TestMethod]
    public void ALongListOfTitlesComesInPiecesTheClientCanRead()
    {
        var owned = Enumerable.Range(1, 300).ToArray();

        var messages = TitleBook.Messages(owned).ToArray();

        Assert.HasCount(3, messages);
        CollectionAssert.AreEqual(Enumerable.Range(1, 127).ToArray(), messages[0].TitleList.ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(128, 127).ToArray(), messages[1].TitleList.ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(255, 46).ToArray(), messages[2].TitleList.ToArray());

        foreach (var message in messages)
        {
            var bytes = Written(message);

            Assert.AreEqual(1, bytes[0]);
            Assert.IsLessThanOrEqualTo((byte)127, bytes[1], "the client takes the count as a signed byte");
            Assert.AreEqual(2 + 4 * message.TitleList.Count + 1, bytes.Length);
        }
    }

    [TestMethod]
    public void AnEmptyListIsStillTold()
    {
        var messages = TitleBook.Messages([]).ToArray();

        Assert.HasCount(1, messages);
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0 }, Written(messages[0]));
    }

    [TestMethod]
    public void TheListIsTheResultThenTheTitlesThenThoseThatAreOpen()
    {
        var bytes = Written(new PersonLoadTitleResponse { TitleList = [7, 8], OpenList = [9], Result = true });

        CollectionAssert.AreEqual(
            new byte[] { 1, 2, 7, 0, 0, 0, 8, 0, 0, 0, 1, 9, 0, 0, 0 },
            bytes);
    }
}
