namespace OpenWorker.Extensions;

/// <summary>
/// The ports the settings may name.
/// </summary>
internal static class PortRange
{
    /// <summary>
    /// The port a server listens on.
    /// </summary>
    public const int ListenMax = 65535;

    /// <summary>
    /// The port a client is sent to. The caches and the protocol keep it in a signed 16-bit field.
    /// </summary>
    public const int AddressMax = short.MaxValue;
}
