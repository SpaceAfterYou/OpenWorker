using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Server.Common;

/// <summary>
/// Whether a game master's client was sent its character record and may answer it with the options it forces.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct ForcedStaffOptionsComponent(bool Expected);

/// <summary>
/// An unchanged client of a game master forces whispers, trades, parties, leagues, friends and looks to "nobody" when it
/// reads its character record, and sends the options at once. A changed client does not, and all the options it sends are
/// the player's own. Only the first options after the record can be the forced ones.
/// </summary>
public static class StaffOptions
{
    /// <summary>
    /// The character record goes to the client now: a game master's next options may be forced.
    /// </summary>
    public static void ExpectForced(World world, Entity player)
    {
        if (world.TryGet(player, out AccessComponent access) && access.Level != AccessLevel.None &&
            world.Has<ForcedStaffOptionsComponent>(player))
        {
            world.Set(player, new ForcedStaffOptionsComponent(true));
        }
    }

    /// <summary>
    /// Whether these options are the first since the record of a game master; the next ones are not.
    /// </summary>
    public static bool TakeExpected(World world, Entity player)
    {
        if (!world.TryGet(player, out ForcedStaffOptionsComponent forced) || !forced.Expected)
        {
            return false;
        }

        world.Set(player, new ForcedStaffOptionsComponent(false));

        return true;
    }
}
