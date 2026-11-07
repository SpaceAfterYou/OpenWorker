using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Persistence;

namespace OpenWorker.GateServer.Gameplay;

public sealed class OptionGameplay(World world, IDbContextFactory<PersistenceContext> factory)
{
    public async Task PushAsync(ServiceHandleContext context, IReadOnlyCollection<byte> options)
    {
        var account = world.Get<ClaimsComponent>(context.Player).Account;

        if (account == 0)
        {
            return;
        }

        await factory
            .SavePersonOptionsAsync(account, options.ToArray(), context.CancellationToken);
    }
}
