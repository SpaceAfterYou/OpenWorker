using System.Text.Json.Serialization;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// A cooldown group that survives the person leaving the server: the item says so (<c>CooldownSaved</c>).
/// </summary>
public sealed class SavedCooldown
{
    public int Group { get; set; }

    /// <summary>
    /// When the group may be used again, seconds since 1970.
    /// </summary>
    public long EndsAt { get; set; }
}

/// <summary>
/// How often the person used a kind of item on the game day, for the kinds the game limits.
/// </summary>
public sealed class ItemUseCount
{
    /// <summary>
    /// The use type of the items of the kind.
    /// </summary>
    public int Type { get; set; }

    public byte Count { get; set; }

    /// <summary>
    /// When the kind was last used, seconds since 1970.
    /// </summary>
    public long UsedAt { get; set; }

    public ItemUseInfoValue ToValue() => new() { Type = Type, Count = Count, UsedAt = UsedAt };
}

/// <summary>
/// What the use of items leaves with a person: the cooldowns of the item groups and how often the limited kinds were used
/// today. Kept as a document of the person (<c>PersonStates</c>, kind <c>ItemUse</c>); only the cooldowns the items save and the
/// counts are written, the other cooldowns end when the person changes server.
/// </summary>
public sealed class ItemUseBook
{
    /// <summary>
    /// The cooldowns the items save.
    /// </summary>
    public List<SavedCooldown> Saved { get; set; } = [];

    public List<ItemUseCount> Uses { get; set; } = [];

    /// <summary>
    /// When the counts were last brought up to date, seconds since 1970.
    /// </summary>
    public long UpdatedAt { get; set; }

    /// <summary>
    /// The places of the equipment (the memory places) the person has opened with items, one bit for each, the first place
    /// being the lowest bit.
    /// </summary>
    public int OpenEquipSlots { get; set; }

    /// <summary>
    /// The cooldowns that end with the server: group and the moment it may be used again, seconds since 1970.
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, long> Running { get; } = [];
}

public static class ItemUseDocument
{
    /// <summary>
    /// The book as the flush of the person writes it.
    /// </summary>
    public static IPersonDocumentSource Source { get; } =
        new PersonDocumentSource<ItemUseComponent>(PersonStateKind.ItemUse, PersonSections.MiscState, e => e.Book);
}
