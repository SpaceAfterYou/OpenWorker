using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class ItemSetupService(World world) :
    IHotspotHandler<ItemSetupMakeRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupMakeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}