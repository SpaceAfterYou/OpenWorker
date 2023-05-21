using OpenWorker.Infrastructure.Gameplay.Cache.Models;
using Redis.OM.Searching;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Extensions;

internal static class IRedisCollectionExtension
{
    internal static IAsyncEnumerable<GateModel> GetOnlineGates(this IRedisCollection<GateModel> @this) => @this.AsAsyncEnumerable().Where(e => e.IsOnline());
}
