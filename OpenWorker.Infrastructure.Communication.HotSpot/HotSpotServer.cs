using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using System.Net;

namespace OpenWorker.Infrastructure.Communication.Hotspot;

internal sealed class HotSpotServer : TcpServer
{
    internal CancellationToken CancellationToken => _entity.Get<CancellationTokenSource>().Token;
    private IServiceProvider ServiceProvider => _entity.Get<IServiceProvider>();

    private readonly Entity _entity;

    public HotSpotServer(IServiceProvider services) : base(IPAddress.Any, 10_000)
    {
        OptionReuseAddress = true;
        OptionDualMode = true;

        var world = services.GetRequiredService<World>();
        _entity = world.CreateEntity();

        _entity.Set(services);
        _entity.Set(new CancellationTokenSource());
    }

    protected override TcpSession CreateSession()
    {
        var scope = ServiceProvider.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<HotSpotSession>();

        session.Entity.Set(scope);

        return session;
    }

    protected override void Dispose(bool disposingManagedResources)
    {
        if (disposingManagedResources)
        {
            _entity.Dispose();
        }

        base.Dispose(disposingManagedResources);
    }
}
