using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// The table of the contents the servers offer, which every server reads when it starts.
/// </summary>
public interface IServerContentStore
{
    /// <summary>
    /// Writes the state of one content, making its row when it has none.
    /// </summary>
    Task SetAsync(ServerContentIndex index, bool state, CancellationToken cancellationToken = default);
}
