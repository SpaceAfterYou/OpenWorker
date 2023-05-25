using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed record AuthService
{
    private readonly IHotSpotSession _session;

    public AuthService(IHotSpotSession session) => _session = session;

    internal async ValueTask Join()
    {

    }
}
