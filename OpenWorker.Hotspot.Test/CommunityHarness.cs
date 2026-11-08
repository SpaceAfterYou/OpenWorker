using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A stand-in for Redis that several servers of one test share.
/// </summary>
internal sealed class MemoryCommunity : ICommunityTransport, IPresenceStore
{
    private readonly ConcurrentDictionary<string, List<Action<ReadOnlyMemory<byte>>>> _handlers = [];
    private readonly ConcurrentDictionary<int, PresenceRecord> _people = [];

    public ValueTask PublishAsync(string channel, ReadOnlyMemory<byte> payload)
    {
        var copy = payload.ToArray();

        if (_handlers.TryGetValue(channel, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                // Redis calls from a thread of its own.
                _ = Task.Run(() => handler(copy));
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask SubscribeAsync(string channel, Action<ReadOnlyMemory<byte>> handler)
    {
        _handlers.GetOrAdd(channel, _ => []).Add(handler);

        return ValueTask.CompletedTask;
    }

    public ValueTask SetAsync(PresenceRecord record, TimeSpan lifetime)
    {
        _people[record.Person] = record;

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(PresenceRecord record)
    {
        if (_people.TryGetValue(record.Person, out var current) && current.Instance == record.Instance)
        {
            _people.TryRemove(record.Person, out _);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<PresenceRecord?> GetAsync(int person)
    {
        return ValueTask.FromResult(_people.GetValueOrDefault(person));
    }

    public ValueTask<PresenceRecord?> GetByNameAsync(string name)
    {
        var key = PresenceRecord.NameKey(name);

        return ValueTask.FromResult(_people.Values.FirstOrDefault(e => PresenceRecord.NameKey(e.Name) == key));
    }

    public ValueTask<IReadOnlyList<PresenceRecord?>> GetManyAsync(IReadOnlyList<int> persons)
    {
        return ValueTask.FromResult<IReadOnlyList<PresenceRecord?>>(persons.Select(e => _people.GetValueOrDefault(e)).ToArray());
    }

    public bool IsOnline(int person) => _people.ContainsKey(person);
}

internal sealed record ReceivedFrame(byte Group, byte Command, byte[] Content);

/// <summary>
/// A player at the other end of a socket, so that what the server sends can be read back.
/// </summary>
internal sealed class TestPlayer : IDisposable
{
    private readonly TcpClient _client;
    private readonly TcpListener _listener;

    public TestPlayer(TestServer server, int person, string name, byte whisper = PersonOptions.AllowAll, int league = 0)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();

        _client = new TcpClient();
        _client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

        var connection = _listener.AcceptTcpClient();

        // The first digit says that the player has saved the options.
        var options = Enumerable.Repeat((byte)'0', 64).ToArray();
        options[0] = (byte)'5';
        options[PersonOptions.Whisper] = (byte)('0' + whisper);

        Person = person;
        Server = server;

        Entity = server.World.Create(
            new ActorComponent(person, ActorType.User),
            new PersonInfoComponent { Name = name, Hero = Hero.Lily },
            new ServerSessionComponent(connection),
            new PersonOptionComponent { Collection = options },
            new LeagueComponent { Id = league, Name = string.Empty, Card = new LeagueComponentCard { Border = 0, Emblem = 0 } });

        _connection = connection;
    }

    private readonly TcpClient _connection;

    public int Person { get; }

    public TestServer Server { get; }

    public Entity Entity { get; }

    public string Name => Server.World.Get<PersonInfoComponent>(Entity).Name;

    /// <summary>
    /// Reads what arrives until nothing more comes for a short while.
    /// </summary>
    public List<ReceivedFrame> Read(int quietMilliseconds = 300)
    {
        var frames = new List<ReceivedFrame>();
        var stream = _client.GetStream();

        _client.ReceiveTimeout = quietMilliseconds;

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        while (true)
        {
            try
            {
                var header = new MessageHeader(reader);
                var payload = reader.ReadBytes(header.ContentLength);

                MessageEncryption.Exchange(payload.AsSpan(), header.Version);

                frames.Add(new ReceivedFrame(payload[0], payload[1], payload[2..]));
            }

            catch (IOException)
            {
                return frames;
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _connection.Dispose();
        _listener.Stop();
    }
}

/// <summary>
/// One server: its own world and loop, joined to the others through the shared <see cref="MemoryCommunity"/>.
/// </summary>
internal sealed class TestServer : IDisposable
{
    private sealed class Source(World world) : IPresenceSource
    {
        public PresenceRecord Describe(Entity player, Guid instance)
        {
            var info = world.Get<PersonInfoComponent>(player);
            var options = world.Get<PersonOptionComponent>(player).Collection;

            return new PresenceRecord(
                instance,
                world.Get<ActorComponent>(player).Identifier,
                info.Name,
                (byte)info.Hero,
                25,
                10003,
                1,
                false,
                PersonOptions.Level(options, PersonOptions.Whisper),
                PersonOptions.Level(options, PersonOptions.OtherInfo));
        }
    }

    public TestServer(MemoryCommunity community, IEnumerable<ICommunityListener>? listeners = null)
    {
        World = World.Create();
        Loop = new WorldLoop(NullLogger<WorldLoop>.Instance);
        Instance = Guid.NewGuid();

        var services = new ServiceCollection();

        foreach (var listener in listeners ?? [])
        {
            services.AddSingleton(listener);
        }

        Network = new CommunityNetwork(
            World,
            Loop,
            Options.Create(new InstanceOptions { Identifier = Instance }),
            community,
            community,
            new Source(World),
            services.BuildServiceProvider(),
            NullLogger<CommunityNetwork>.Instance);

        Loop.InvokeAsync(() => Network.StartAsync()).GetAwaiter().GetResult();
    }

    public World World { get; }

    public WorldLoop Loop { get; }

    public Guid Instance { get; }

    public CommunityNetwork Network { get; }

    public TestPlayer Join(int person, string name, byte whisper = PersonOptions.AllowAll, int league = 0)
    {
        var player = new TestPlayer(this, person, name, whisper, league);

        Run(() => Network.EnterAsync(player.Entity).AsTask());

        return player;
    }

    public void Leave(TestPlayer player)
    {
        Run(() => Network.OnSessionClosedAsync(player.Entity).AsTask());
    }

    public void Run(Func<Task> action)
    {
        Loop.InvokeAsync(() => new ValueTask(action())).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        World.Dispose();
    }
}
