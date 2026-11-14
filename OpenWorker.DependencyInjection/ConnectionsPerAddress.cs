using System.Net;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// The open connections of each address, held under a limit. An address counts as the client's: an IPv4 address that
/// comes mapped into IPv6 is the same address.
/// </summary>
internal sealed class ConnectionsPerAddress(int limit)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<IPAddress, Entry> _open = [];

    private sealed class Entry
    {
        public int Count;

        /// <summary>
        /// A refusal was reported since the address was last under its limit.
        /// </summary>
        public bool Reported;
    }

    public static IPAddress? AddressOf(EndPoint? endPoint)
    {
        return endPoint is IPEndPoint { Address: var address }
            ? address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address
            : null;
    }

    /// <summary>
    /// Counts one more connection of the address, or refuses it when the address holds the limit already. A refusal is
    /// to be reported only when <paramref name="report"/> is true: once each time the address reaches its limit.
    /// </summary>
    public bool TryEnter(IPAddress? address, out bool report)
    {
        report = false;

        if (limit <= 0 || address is null)
        {
            return true;
        }

        lock (_lock)
        {
            if (!_open.TryGetValue(address, out var entry))
            {
                _open[address] = entry = new Entry();
            }

            if (entry.Count >= limit)
            {
                report = !entry.Reported;
                entry.Reported = true;

                return false;
            }

            entry.Count++;

            return true;
        }
    }

    /// <summary>
    /// A connection of the address that was let in has ended.
    /// </summary>
    public void Exit(IPAddress? address)
    {
        if (limit <= 0 || address is null)
        {
            return;
        }

        lock (_lock)
        {
            if (!_open.TryGetValue(address, out var entry))
            {
                return;
            }

            entry.Count--;
            entry.Reported = false;

            if (entry.Count <= 0)
            {
                _open.Remove(address);
            }
        }
    }
}
