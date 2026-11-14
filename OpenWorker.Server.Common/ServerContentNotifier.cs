using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Server.Common;

/// <summary>
/// Tells the players of a district or a maze that a content was switched: the client in the game reads the contents only
/// from 01/07. A cash shop that opens gives each login the window's contents before the notice, and an attendance that
/// opens gives each person the attendance of the day.
/// </summary>
public sealed class ServerContentNotifier(
    World world,
    ServerContent contents,
    CommunityNetwork network,
    CashShopWindow cashShop,
    AttendanceManager attendance) : IServerContentListener
{
    public async ValueTask OnSwitchedAsync(ServerContentIndex index, bool state, CancellationToken cancellationToken)
    {
        var update = new SystemServerOptionUpdateResponse { Contents = contents };
        var players = network.Online.ToArray();

        if (index == ServerContentIndex.CashShop && state)
        {
            await cashShop.OpenAsync(players, update, cancellationToken);
            return;
        }

        foreach (var player in players)
        {
            if (world.IsAlive(player) && world.TryGet(player, out ServerSessionComponent session))
            {
                session.Send(update);
            }
        }

        if (index == ServerContentIndex.Attendance && state)
        {
            foreach (var player in players)
            {
                if (world.IsAlive(player))
                {
                    await attendance.RefreshAsync(player, cancellationToken);
                }
            }
        }
    }
}
