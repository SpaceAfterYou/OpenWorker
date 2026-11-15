using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A part of a monster that blows can break off.
/// </summary>
/// <param name="Row">The row of the part in the table of broken parts.</param>
/// <param name="Bones">The number of the hit bones that belong to the part; a blow on another bone does not touch it.</param>
public readonly record struct MonsterPart(int Row, byte Bones, int Health, int MaxHealth)
{
    public bool IsBroken => MaxHealth > 0 && Health <= 0;
}

/// <summary>
/// The two parts a monster can have. A monster with a part takes no blow on its own life: only its parts do.
/// </summary>
public readonly record struct MonsterParts(MonsterPart First, MonsterPart Second)
{
    public bool Any => First.MaxHealth > 0 || Second.MaxHealth > 0;
}

/// <summary>
/// What a monster turns into when a part of it breaks, after the animation it plays first.
/// </summary>
public readonly record struct PartBreak(int Monster, string Animation);

/// <summary>
/// The rules of the parts of monsters: what parts a monster has, what a blow does to them and what a broken part turns the
/// monster into.
/// </summary>
public sealed class MonsterPartRules
{
    private const byte PartsMonster = 11;
    private const float HealthShareScale = 0.0001f;

    private readonly Dictionary<int, (int First, int Second, float Scale)> _monsters;
    private readonly RowIndex<int, MonsterBrokenPartsRow> _parts;

    public MonsterPartRules(ReadOnlyCollection<MonsterRow> monsters, ReadOnlyCollection<MonsterBrokenPartsRow> parts)
    {
        _monsters = monsters
            .DistinctBy(e => e.Id)
            .ToDictionary(e => e.Id, e => (e.Type == PartsMonster ? e.PartsId2 : 0, e.Type == PartsMonster ? e.PartsId3 : 0, e.Scale));
        _parts = parts.DistinctBy(e => e.Id).IndexBy(e => e.Id);
    }

    /// <summary>
    /// How much larger than its model the monster is drawn, which moves its bones as far.
    /// </summary>
    public float ScaleOf(int monster)
    {
        return _monsters.TryGetValue(monster, out var row) && float.IsFinite(row.Scale) && row.Scale > 0f ? row.Scale : 1f;
    }

    /// <summary>
    /// The parts of a monster that has just appeared with the given life: each the share of that life its row gives.
    /// </summary>
    public MonsterParts Create(int monster, int health)
    {
        if (!_monsters.TryGetValue(monster, out var row))
        {
            return default;
        }

        return new MonsterParts(Part(row.First, health), Part(row.Second, health));
    }

    private MonsterPart Part(int id, int health)
    {
        if (id == 0 || !_parts.TryGet(id, out var row))
        {
            return default;
        }

        var life = (int)(health * (row.HealthShare * HealthShareScale));

        return new MonsterPart(row.Id, (byte)Math.Clamp(row.Bones, byte.MinValue, byte.MaxValue), life, life);
    }

    /// <summary>
    /// A blow that lands on a bone of a part takes its damage from that part, down to nothing. A blow on no part changes nothing.
    /// </summary>
    /// <param name="struck">Whether the blow landed on a part, so the clients are told the life of the parts.</param>
    public static MonsterParts Hit(MonsterParts parts, byte bones, int damage, out bool struck)
    {
        struck = false;

        if (damage <= 0 || bones == 0)
        {
            return parts;
        }

        var first = parts.First;
        var second = parts.Second;

        if (first.Bones == bones)
        {
            first = first with { Health = Math.Clamp(first.Health - damage, 0, first.MaxHealth) };
            struck = true;
        }

        if (second.Bones == bones)
        {
            second = second with { Health = Math.Clamp(second.Health - damage, 0, second.MaxHealth) };
            struck = true;
        }

        return new MonsterParts(first, second);
    }

    /// <summary>
    /// What a broken part turns the monster into: the monster of the part while the other part stands, the one both parts lead
    /// to when the other is gone too. Null while no part is broken, or when the row of the part is not known.
    /// </summary>
    public PartBreak? BreakOf(MonsterParts parts)
    {
        MonsterPart broken;
        MonsterPart other;

        if (parts.First.IsBroken)
        {
            (broken, other) = (parts.First, parts.Second);
        }
        else if (parts.Second.IsBroken)
        {
            (broken, other) = (parts.Second, parts.First);
        }
        else
        {
            return null;
        }

        if (!_parts.TryGet(broken.Row, out var row))
        {
            return null;
        }

        var (monster, animation) = other.Health > 0 ? (row.Spawn, row.SpawnAnimation) : (row.Link, row.LinkAnimation);

        return monster == 0 ? null : new PartBreak(monster, animation);
    }
}
