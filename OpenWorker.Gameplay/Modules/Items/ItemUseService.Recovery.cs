using Arch.Core;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The kind of effect in the item row that names a buff and an aura.
    private const int BuffEffect = 1;
    private const int AuraEffect = 2;

    // What a kind of item may do where, in the bits of the use state of its classify row.
    private const int UsableInDistrict = 1;
    private const int UsableInMaze = 2;

    // The sub types of the item row of the revive items: a buff that lets a person get up, and a call that gets it up.
    private const int ReviveBuffSubType = 1;
    private const int ReviveCallSubType = 2;

    // The item did nothing: what the original answers when the effect of a consumable fails.
    private const ushort EffectFailed = 52606;

    /// <summary>
    /// A potion or the like: it gives its buff or its aura to the person, or, for a revive item, gets the fallen up. A place
    /// the kind of item is not for and the infinite tower are refused, and a revive item where nobody may get up.
    /// </summary>
    private ItemUseResult UseEffect(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify, ItemPlace place)
    {
        if (classify.Group != ConsumableGroup)
        {
            return ItemUseResult.Refused();
        }

        // Where the kind may be used: the place has to take items, and the use state of the kind has to name the place.
        var bit = place.InMaze ? UsableInMaze : UsableInDistrict;

        if (!place.ItemsAllowed || (classify.UseState & bit) == 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.WrongPlace);
        }

        if (place.Infinite)
        {
            return ItemUseResult.Refused(ItemUseCodes.InfiniteTower);
        }

        var revival = ReviveKindOf(row);

        if (revival is { } kind && (revive is null ? ItemUseCodes.NoRevive : revive.Check(player, kind)) is { } refusal)
        {
            return ItemUseResult.Refused(refusal);
        }

        var given = revival switch
        {
            ReviveItemKind.Call or ReviveItemKind.PartyCall => GetUp(player, revival.Value),
            _ when row.EffectType == BuffEffect => _field.GiveBuff(player, row.EffectValue),
            _ when row.EffectType == AuraEffect => _field.GiveAura(player, row.EffectValue),
            _ => false
        };

        if (!given)
        {
            return ItemUseResult.Refused(EffectFailed);
        }

        // The stack may go with its last item: the motion names the slot it was in.
        var slot = item.Slot;

        inventory.Spend(player, item, 1);
        achievements?.Count(player, AchievementType.ItemUsed);

        return ItemUseResult.Used(item.Item, slot, motion: true);
    }

    /// <summary>
    /// The kind of revive item a row is, by its sub type and its effect; null for an item that is no revive item.
    /// </summary>
    private static ReviveItemKind? ReviveKindOf(ItemRow row) => (row.SubType, row.EffectType) switch
    {
        (ReviveCallSubType, BuffEffect) => ReviveItemKind.Call,
        (ReviveCallSubType, AuraEffect) => ReviveItemKind.PartyCall,
        (ReviveBuffSubType, BuffEffect) => ReviveItemKind.Buff,
        (ReviveBuffSubType, AuraEffect) => ReviveItemKind.PartyBuff,
        _ => null
    };

    private bool GetUp(Entity player, ReviveItemKind kind)
    {
        revive!.GetUp(player, kind);

        return true;
    }
}
