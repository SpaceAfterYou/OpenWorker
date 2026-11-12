using System.Globalization;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

/// <summary>
/// The room of an account while somebody is in it: which maze server runs it and which instance on it, so that everybody who
/// comes, the owner and the visitors from any district or room, is sent into the same one. The server that sends the first
/// person in writes it; the maze server takes it back when the last one leaves. It also expires, in case the server stops.
/// </summary>
[Document(StorageType = StorageType.Hash)]
public sealed class MyRoomCache
{
    /// <summary>
    /// How long the record of a room lives at most without its maze server taking it back.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromHours(6);

    // The index of the relay needs a field to be made; the record is only ever read by its key.
    [Indexed]
    [RedisIdField]
    public required string Key { get; init; }

    [RedisField]
    public required int Account { get; init; }

    [RedisField]
    public required short Gate { get; init; }

    [RedisField]
    public required Guid Instance { get; init; }

    /// <summary>
    /// The identifier of the maze server, which names its record (<see cref="MazeCache"/>): without the record the server is gone.
    /// </summary>
    [RedisField]
    public required Guid Server { get; init; }

    [RedisField]
    public required string Host { get; init; }

    [RedisField]
    public required short Port { get; init; }

    /// <summary>
    /// The owner arranges the room: nobody else may come in. The maze server keeps it up to date; it decides at the door.
    /// </summary>
    [RedisField]
    public bool Arranging { get; set; }

    /// <summary>
    /// How many others than the owner are in the room, as the maze server last told.
    /// </summary>
    [RedisField]
    public int Visitors { get; set; }

    /// <summary>
    /// The key of the room of an account on a gate.
    /// </summary>
    public static string KeyOf(int account, short gate) => string.Create(CultureInfo.InvariantCulture, $"{gate}-{account}");
}
