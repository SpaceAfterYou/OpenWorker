using MassTransit;
using Microsoft.Extensions.Configuration;
using Redis.OM;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed class ThisInstance
{
    internal ushort District { get; }
    internal IReadOnlyCollection<InstanceChannel> Channels { get; }

    public ThisInstance(IConfiguration configuration)
    {
        District = configuration.GetValue<ushort>("District:Id");

        var min = configuration.GetValue<ushort>("District:MinChannel");
        var max = configuration.GetValue<ushort>("District:MaxChannel");

        var sessionPerChannel = configuration.GetValue<ushort>("District:SessionPerChannel");

        Channels = Enumerable.Range(min, max - min).Select(e => new InstanceChannel(sessionPerChannel) { Id = (ushort)e }).ToArray();
    }
}
