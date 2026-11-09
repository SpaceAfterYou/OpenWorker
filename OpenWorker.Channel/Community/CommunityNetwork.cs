using Arch.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What a server does when somebody comes or goes: friends and the league tell their people, for one.
/// </summary>
public interface ICommunityListener
{
    ValueTask OnEnteredAsync(Entity player, PresenceRecord person);

    ValueTask OnLeftAsync(Entity player, PresenceRecord person);
}

/// <summary>
/// A part of the game that has something to say to the other servers. It is attached once, when the server starts.
/// </summary>
public interface ICommunityModule
{
    void Attach(CommunityNetwork network);
}

/// <param name="Name">What the sender called it.</param>
/// <param name="Persons">Those of the targets who are on this server, or empty if it was meant for everybody.</param>
public readonly record struct CommunityEvent(string Name, IReadOnlyList<int> Persons, ReadOnlyMemory<byte> Body);

/// <summary>
/// The people of this server and the way to the people of the others. The district and the maze servers are many, and a
/// whisper, a friend request or a league message has to reach a person wherever they are: a server writes the answer
/// once, and the one the person is connected to hands it to the client. Everything runs on the world loop.
/// </summary>
public sealed partial class CommunityNetwork(
    World world,
    WorldLoop loop,
    IOptions<InstanceOptions> instance,
    IPresenceStore presence,
    ICommunityTransport transport,
    IPresenceSource source,
    IServiceProvider services,
    ILogger<CommunityNetwork> logger
) :
    ISessionCloseHandler
{
    public const string AllChannel = "ow:all";

    public static TimeSpan PresenceLifetime { get; } = TimeSpan.FromSeconds(90);
    public static TimeSpan PresenceRenewal { get; } = TimeSpan.FromSeconds(30);

    private Dictionary<int, Entity> Players { get; } = [];
    private Dictionary<string, int> Names { get; } = [];
    private Dictionary<int, PresenceRecord> Records { get; } = [];
    private Dictionary<string, Func<CommunityEvent, ValueTask>> Handlers { get; } = [];

    private IEnumerable<ICommunityListener> Listeners => services.GetServices<ICommunityListener>();

    public Guid Instance { get; } = instance.Value.Identifier;

    public static string InstanceChannel(Guid instance) => "ow:i:" + instance.ToString("N");

    public int Count => Players.Count;

    public IReadOnlyCollection<Entity> Online => Players.Values;

#region Local people

    public bool TryGet(int person, out Entity player)
    {
        return Players.TryGetValue(person, out player) && world.IsAlive(player);
    }

    public bool TryGetByName(string name, out Entity player)
    {
        if (Names.TryGetValue(PresenceRecord.NameKey(name), out var person))
        {
            return TryGet(person, out player);
        }

        player = default;

        return false;
    }

    /// <summary>
    /// Makes a person known to the others. Called once, when the client is in the game.
    /// </summary>
    public async ValueTask EnterAsync(Entity player)
    {
        var record = source.Describe(player, Instance);

        Players[record.Person] = player;
        Names[PresenceRecord.NameKey(record.Name)] = record.Person;
        Records[record.Person] = record;

        await TryAsync(() => presence.SetAsync(record, PresenceLifetime).AsTask(), "publish a person");

        foreach (var listener in Listeners)
        {
            await TryAsync(() => listener.OnEnteredAsync(player, record).AsTask(), "tell about a person coming");
        }
    }

    /// <summary>
    /// Brings what the others know about a person up to date at once, after a change they read from it (its options),
    /// instead of at the next renewal.
    /// </summary>
    public async ValueTask RefreshAsync(Entity player)
    {
        if (!world.Has<ActorComponent>(player))
        {
            return;
        }

        var person = world.Get<ActorComponent>(player).Identifier;

        if (!Players.TryGetValue(person, out var known) || known != player)
        {
            return;
        }

        var record = source.Describe(player, Instance);

        Records[person] = record;

        await TryAsync(() => presence.SetAsync(record, PresenceLifetime).AsTask(), "renew a person");
    }

    public async ValueTask OnSessionClosedAsync(Entity player)
    {
        if (!world.Has<ActorComponent>(player))
        {
            return;
        }

        var person = world.Get<ActorComponent>(player).Identifier;

        // Only the connection that was announced may take the person away.
        if (!Players.TryGetValue(person, out var known) || known != player || !Records.Remove(person, out var record))
        {
            return;
        }

        Players.Remove(person);
        Names.Remove(PresenceRecord.NameKey(record.Name));

        await TryAsync(() => presence.RemoveAsync(record).AsTask(), "withdraw a person");

        foreach (var listener in Listeners)
        {
            await TryAsync(() => listener.OnLeftAsync(player, record).AsTask(), "tell about a person leaving");
        }
    }

    /// <summary>
    /// Keeps the entries from expiring and brings them up to date with a channel or a level that has changed.
    /// </summary>
    public async ValueTask RenewAsync()
    {
        var updates = new List<PresenceRecord>(Players.Count);

        foreach (var (person, player) in Players)
        {
            if (world.IsAlive(player))
            {
                var record = source.Describe(player, Instance);

                Records[person] = record;
                updates.Add(record);
            }
        }

        await TryAsync(
            () => Task.WhenAll(updates.Select(e => presence.SetAsync(e, PresenceLifetime).AsTask())),
            "renew the people");
    }

#endregion Local people

#region Finding people

    public ValueTask<PresenceRecord?> FindAsync(int person) => presence.GetAsync(person);

    public ValueTask<PresenceRecord?> FindByNameAsync(string name) => presence.GetByNameAsync(name);

    public ValueTask<IReadOnlyList<PresenceRecord?>> FindManyAsync(IReadOnlyList<int> persons) => presence.GetManyAsync(persons);

#endregion Finding people

#region Sending

    public ValueTask SendAsync(int person, IResponseHotspotMessage message)
    {
        return SendAsync([person], message);
    }

    /// <summary>
    /// Sends the message to each of the persons that is online, on this server or on another.
    /// </summary>
    public async ValueTask SendAsync(IEnumerable<int> persons, IResponseHotspotMessage message)
    {
        var remote = new List<int>();

        foreach (var person in persons.Distinct())
        {
            if (TryGet(person, out var player))
            {
                world.Get<ServerSessionComponent>(player).Send(message);
            }

            else
            {
                remote.Add(person);
            }
        }

        if (remote.Count == 0)
        {
            return;
        }

        foreach (var (instance, targets) in await GroupByInstanceAsync(remote))
        {
            var envelope = CommunityEnvelope.ForPacket(Instance, targets, message);

            await TryAsync(() => transport.PublishAsync(InstanceChannel(instance), envelope.ToBytes()).AsTask(), "send a message");
        }
    }

    /// <summary>
    /// Sends the message to everybody connected to any server.
    /// </summary>
    public async ValueTask BroadcastAsync(IResponseHotspotMessage message)
    {
        foreach (var player in Players.Values)
        {
            if (world.IsAlive(player))
            {
                world.Get<ServerSessionComponent>(player).Send(message);
            }
        }

        var envelope = CommunityEnvelope.ForPacket(Instance, [], message);

        await TryAsync(() => transport.PublishAsync(AllChannel, envelope.ToBytes()).AsTask(), "broadcast a message");
    }

    /// <summary>
    /// Has the servers of the persons run the event: this one for those who are here, the others for theirs. With no persons
    /// every server runs it once.
    /// </summary>
    public async ValueTask PublishAsync(IEnumerable<int> persons, string name, ReadOnlyMemory<byte> body)
    {
        var targets = persons.Distinct().ToArray();

        if (targets.Length == 0)
        {
            await RunAsync(new CommunityEvent(name, [], body));

            var envelope = new CommunityEnvelope(CommunityEnvelopeKind.Event, Instance, [], name, body);

            await TryAsync(() => transport.PublishAsync(AllChannel, envelope.ToBytes()).AsTask(), "publish an event");

            return;
        }

        var local = targets.Where(Players.ContainsKey).ToArray();
        var remote = targets.Except(local).ToArray();

        if (local.Length != 0)
        {
            await RunAsync(new CommunityEvent(name, local, body));
        }

        if (remote.Length == 0)
        {
            return;
        }

        foreach (var (instance, group) in await GroupByInstanceAsync(remote))
        {
            var envelope = new CommunityEnvelope(CommunityEnvelopeKind.Event, Instance, group, name, body);

            await TryAsync(() => transport.PublishAsync(InstanceChannel(instance), envelope.ToBytes()).AsTask(), "publish an event");
        }
    }

    public void On(string name, Func<CommunityEvent, ValueTask> handler)
    {
        Handlers[name] = handler;
    }

    private async ValueTask<Dictionary<Guid, int[]>> GroupByInstanceAsync(IReadOnlyList<int> persons)
    {
        var records = await TryAsync(() => presence.GetManyAsync(persons).AsTask(), "look the people up");

        if (records is null)
        {
            return [];
        }

        return records
            .Where(e => e is not null && e.Instance != Instance)
            .GroupBy(e => e!.Instance)
            .ToDictionary(e => e.Key, e => e.Select(x => x!.Person).ToArray());
    }

#endregion Sending

#region Receiving

    /// <summary>
    /// Starts listening for what the other servers send to this one.
    /// </summary>
    public async ValueTask StartAsync()
    {
        await transport.SubscribeAsync(InstanceChannel(Instance), OnReceived);
        await transport.SubscribeAsync(AllChannel, OnReceived);
    }

    private void OnReceived(ReadOnlyMemory<byte> bytes)
    {
        if (!CommunityEnvelope.TryRead(bytes, out var envelope) || envelope.Origin == Instance)
        {
            return;
        }

        loop.Post(_ => _ = DeliverAsync(envelope), null);
    }

    private async Task DeliverAsync(CommunityEnvelope envelope)
    {
        try
        {
            if (envelope.Kind == CommunityEnvelopeKind.Packet)
            {
                Deliver(envelope);
            }

            else
            {
                await RunAsync(new CommunityEvent(envelope.Name, envelope.Targets.Where(Players.ContainsKey).ToArray(), envelope.Body));
            }
        }

        catch (Exception e)
        {
            LogDeliveryFailed(logger, e);
        }
    }

    private void Deliver(CommunityEnvelope envelope)
    {
        if (!RawResponse.TryCreate(envelope.Body, out var response))
        {
            return;
        }

        IEnumerable<Entity> receivers = envelope.Targets.Count == 0
            ? Players.Values.ToArray()
            : envelope.Targets.Where(Players.ContainsKey).Select(e => Players[e]).ToArray();

        // Written once for everyone it goes to.
        using var frame = Frame.Encode(response);

        if (frame is null)
        {
            return;
        }

        foreach (var player in receivers)
        {
            if (world.IsAlive(player))
            {
                world.Get<ServerSessionComponent>(player).Send(frame);
            }
        }
    }

    private async ValueTask RunAsync(CommunityEvent value)
    {
        if (Handlers.TryGetValue(value.Name, out var handler))
        {
            await TryAsync(() => handler(value).AsTask(), $"run the event {value.Name}");
        }
    }

#endregion Receiving

    /// <summary>
    /// A server that cannot reach Redis for a moment must not drop the person who asked.
    /// </summary>
    private async ValueTask TryAsync(Func<Task> action, string what)
    {
        try
        {
            await action();
        }

        catch (Exception e)
        {
            LogFailed(logger, e, what);
        }
    }

    private async ValueTask<T?> TryAsync<T>(Func<Task<T>> action, string what)
    {
        try
        {
            return await action();
        }

        catch (Exception e)
        {
            LogFailed(logger, e, what);

            return default;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle a message from another server.")]
    private static partial void LogDeliveryFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not {What}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, string what);
}
