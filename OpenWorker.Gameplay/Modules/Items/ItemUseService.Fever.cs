using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The bit of the use state of a kind whose items act where they are picked up and never reach the bags.
    private const int UsedOnPickUp = 16;

    /// <summary>
    /// A fever item (attack, defence, speed, a heal) acts where it is picked up in a maze: it never comes from a bag, so its
    /// use from one is refused.
    /// </summary>
    private static ItemUseResult UseFever() => ItemUseResult.Refused();

    /// <summary>
    /// An item picked up from the ground whose kind acts at once: it gives its buff or its aura to the person and does not go
    /// into the bags. No check of level, class, cooldown or place is made, and the item is taken even when its effect does
    /// not hold. False for an item that goes into the bags.
    /// </summary>
    public bool UseOnPickUp(Entity player, int item)
    {
        if (catalog.Classify(item) is not { } classify || (classify.UseState & UsedOnPickUp) == 0 || !catalog.TryGet(item, out var row))
        {
            return false;
        }

        _ = row.EffectType switch
        {
            BuffEffect => _field.GiveBuff(player, row.EffectValue),
            AuraEffect => _field.GiveAura(player, row.EffectValue),
            _ => false
        };

        return true;
    }
}
