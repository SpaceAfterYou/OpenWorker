using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The district as the place items act in: a potion whose kind may be used in a city gives its buff here, if its row lets
/// the buff be in a district. A district has no auras.
/// </summary>
public sealed class DistrictItemBuffs(DistrictBuffs buffs) : IItemField
{
    public ItemPlace PlaceOf(Entity player) => ItemPlace.District;

    public bool GiveBuff(Entity player, int buff) => buffs.Give(player, buff);
}
