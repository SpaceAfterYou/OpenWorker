using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Channel;

public sealed partial class WorldManager(
    IOptions<GateOptions> gate,
    IOptions<MazeEndpointOptions> mazeEndpoint,
    BatchManager provider,
    IRedisCollection<DistrictReserveCache> districtReserveCache,
    World ecs,
    IRedisCollection<ChannelCache> channels,
    IRedisCollection<DistrictCache> districts,
    IRedisCollection<MazeReserveCache> mazeReserves,
    MazeDirectory mazes,
    IPersonFlush persons,
    ILogger<WorldManager> logger,
    WorldMazeRoutes? worlds = null)
{
    /// <summary>
    /// How long a move between servers stays valid. The client reconnects right after the response,
    /// so an expired reservation means that it never came.
    /// </summary>
    public static TimeSpan ReservationLifetime { get; } = TimeSpan.FromSeconds(60);

    private short Gate { get; } = gate.Value.Id;
    /// <summary>
    /// Where players go when no maze server of the gate has registered itself.
    /// </summary>
    private (string Host, short Port) DefaultMaze { get; } = (mazeEndpoint.Value.Host, (short)mazeEndpoint.Value.Port);

    /// <summary>
    /// The maze server of this gate with the fewest players: one call to the sorted set of <see cref="MazeDirectory"/>.
    /// </summary>
    private async ValueTask<(string Host, short Port)> GetMazeAsync()
    {
        return await mazes.PickAsync(Gate) ?? DefaultMaze;
    }

    /// <summary>
    /// How an attempt to enter a location ended.
    /// </summary>
    private enum EnterOutcome
    {
        Entered,

        /// <summary>
        /// The client asked for something that cannot be entered.
        /// </summary>
        Failed,

        /// <summary>
        /// The location exists, but no server is running it.
        /// </summary>
        Unavailable
    }

    /// <summary>
    /// Enters the location or tells the client that the entry failed, so that it does not wait forever.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, short location, int jump)
    {
        var outcome = await EnterCoreAsync(player, location, jump);

        // The player may have left while the servers were looked up.
        if (outcome is EnterOutcome.Entered || !ecs.IsAlive(player))
        {
            return;
        }

        var result = outcome is EnterOutcome.Unavailable
            ? EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable)
            : EnterMapResultValue.Error;

        ecs.Get<ServerSessionComponent>(player).Send(new WorldEnterResponse { Map = result });
    }

    public async ValueTask<bool> TryEnter(Entity player, short location, int jump = 0)
    {
        return await EnterCoreAsync(player, location, jump) is EnterOutcome.Entered;
    }

    private async ValueTask<EnterOutcome> EnterCoreAsync(Entity player, short location, int jump)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type))
        {
            LogUnknownLocation(location);

            return EnterOutcome.Failed;
        }

        if (!TryGetStart(batch, location, jump, out var start))
        {
            LogNoStart(location, jump);

            return EnterOutcome.Failed;
        }

        switch (type)
        {
            case BatchType.Maze:
                return await EnterMaze(player, location, start) ? EnterOutcome.Entered : EnterOutcome.Unavailable;

            case BatchType.District:
                return await TryEnterDistrict(player, location, start);

            case BatchType.None:
            default:
                return EnterOutcome.Failed;
        }
    }

    public async ValueTask<bool> TryEnterMaze(Entity player, short location)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type) ||
            type != BatchType.Maze ||
            !TryGetStart(batch, location, 0, out var start))
        {
            return false;
        }

        return await EnterMaze(player, location, start);
    }

    /// <summary>
    /// Finds the requested start point, or the default one of the location when none was requested
    /// or the requested one does not exist. A batch several mazes share (the stages of the infinite tower) has one start point
    /// for all of them, named after the first: a map with only one start point is entered by it.
    /// </summary>
    private static bool TryGetStart(VBatchFile batch, short location, int jump, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StartEventBox? start)
    {
        var starts = batch.EventBox.StartEvents;

        if ((jump != 0 && starts.TryGet(jump, out start)) || starts.TryGet(location * 100 + 1, out start))
        {
            return true;
        }

        start = starts.Count == 1 ? starts[0] : null;

        return start is not null;
    }

    /// <summary>
    /// Whether the location is a maze the client can be sent into.
    /// </summary>
    public bool IsMaze(short location)
    {
        return provider.TryGetAndCache(location, out _, out var type) && type == BatchType.Maze;
    }

    /// <summary>
    /// The maze server of this gate with the fewest players and its identifier, or the configured one with an empty
    /// identifier when none has registered.
    /// </summary>
    public async ValueTask<(Guid Server, string Host, short Port)> PickMazeServerAsync()
    {
        return await mazes.PickServerAsync(Gate) ?? (Guid.Empty, DefaultMaze.Host, DefaultMaze.Port);
    }

    /// <summary>
    /// Sends the player into an instance of a maze on the given maze server: the instance a party shares, which the maze
    /// server opens for the first member to come and the others join. False when the location has no start to enter by.
    /// </summary>
    public async ValueTask<bool> SendToMazeAsync(Entity player, short location, int jump, (string Host, short Port) maze, Guid instance, int party, int members = 0)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type) ||
            type != BatchType.Maze ||
            !TryGetStart(batch, location, jump, out var start))
        {
            return false;
        }

        await SendToMazeAsync(player, location, start, maze, instance, party, members);

        return true;
    }

    /// <summary>
    /// Sends the player into a maze of its own, or into the instance of its channel of an open maze of the world. False when
    /// every channel of the open maze is full.
    /// </summary>
    private async ValueTask<bool> EnterMaze(Entity player, short location, StartEventBox start)
    {
        if (worlds is not null && worlds.IsWorld(location))
        {
            var picked = await PickMazeServerAsync();

            if (!ecs.IsAlive(player))
            {
                return true;
            }

            if (await worlds.RouteAsync(player, location, picked) is not var (route, channel))
            {
                return !ecs.IsAlive(player);
            }

            if (ecs.IsAlive(player))
            {
                await SendToMazeAsync(player, location, start, (route.Host, route.Port), route.Instance, 0, 0, world: (byte)channel);
            }

            return true;
        }

        var maze = await GetMazeAsync();

        // The player may have left while the servers were looked up.
        if (!ecs.IsAlive(player))
        {
            return true;
        }

        await SendToMazeAsync(player, location, start, maze, Guid.Empty, 0, 0);

        return true;
    }

    /// <summary>
    /// Sends the player into the instance of the room of an account, the room being played in a maze; <paramref name="back"/> is
    /// the district the player goes back to when it leaves; <paramref name="friend"/> tells the room that the owner is a friend
    /// of the player. False when the location has no start to enter by.
    /// </summary>
    public async ValueTask<bool> SendToRoomAsync(Entity player, short location, (string Host, short Port) maze, Guid instance, int owner, short back,
        bool friend = false)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type) ||
            type != BatchType.Maze ||
            !TryGetStart(batch, location, 0, out var start))
        {
            return false;
        }

        await SendToMazeAsync(player, location, start, maze, instance, 0, 0, owner, back, friend);

        return true;
    }

    private async ValueTask SendToMazeAsync(Entity player, short location, StartEventBox start, (string Host, short Port) maze, Guid instance, int party, int members, int room = 0, short back = 0,
        bool friend = false, byte world = 0)
    {
        var session = ecs.Get<ServerSessionComponent>(player);

        // An open maze of the world is played by channel, and its map says which.
        var map = new MapValue
        {
            Channel = world,
            Location = location,
            Server = Gate
        };

        var place = new WorldValue
        {
            Location = location,
            Position = start.GetPosition(),
            Rotation = start.Rotation,
            Map = map
        };

        var enter = new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = ecs.Get<ActorComponent>(player).Identifier,
                Account = ecs.Get<ClaimsComponent>(player),
                World = place,
                Jump = start.Id,
                Address = maze.Host,
                Port = maze.Port,
                Type = EnterMapType.Maze,
                Map = map
            },
            ChangeServer = true,
            ChangeType = ChangeServerType.EnterMap
        };

        var cache = new MazeReserveCache
        {
            Account = ecs.Get<ClaimsComponent>(player),
            Location = location,
            Person = ecs.Get<ActorComponent>(player).Identifier,
            X = place.Position.X,
            Y = place.Position.Y,
            Z = place.Position.Z,
            R = place.Rotation,
            Jump = start.Id,
            Instance = instance,
            World = world,
            Party = party,
            Members = (byte)Math.Clamp(members, 0, byte.MaxValue),
            Room = room,
            Return = back,
            Friend = friend
        };

        // The maze loads the person from the database: what this server has must be there first.
        await persons.FlushAsync(player);

        await mazeReserves
            .InsertAsync(cache, ReservationLifetime);

        session.Send(new WorldEnterResponse { Map = enter });
    }

    private async ValueTask<EnterOutcome> TryEnterDistrict(Entity player, short location, StartEventBox start)
    {
        var session = ecs.Get<ServerSessionComponent>(player);

        if (await GetDistrictAsync(location) is not { } placement)
        {
            return EnterOutcome.Unavailable;
        }

        // The player may have left while the servers were looked up.
        if (!ecs.IsAlive(player))
        {
            return EnterOutcome.Failed;
        }

        var (district, channel) = placement;

        var map = new MapValue
        {
            Channel = channel.Identifier,
            Location = location,
            Server = Gate
        };

        var world = new WorldValue
        {
            Location = location,
            Position = start.GetPosition(),
            Rotation = start.Rotation,
            Map = map
        };

        var enter = new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = ecs.Get<ActorComponent>(player).Identifier,
                Account = ecs.Get<ClaimsComponent>(player),
                World = world,
                Jump = start.Id,
                Address = district.Host,
                Port = district.Port,
                Type = EnterMapType.EnterDistrict,
                Map = map
            },
            ChangeServer = true,
            ChangeType = ChangeServerType.EnterDistrict
        };

        var cache = new DistrictReserveCache
        {
            Account = ecs.Get<ClaimsComponent>(player),
            Person = ecs.Get<ActorComponent>(player).Identifier,
            Channel = channel.Guid,
            District = district.Guid,
            X = world.Position.X,
            Y = world.Position.Y,
            Z = world.Position.Z,
            R = world.Rotation,
            Jump = start.Id
        };

        // The district loads the person from the database: what this server has must be there first.
        await persons.FlushAsync(player);

        await districtReserveCache
            .InsertAsync(cache, ReservationLifetime);

        session.Send(new WorldEnterResponse { Map = enter });

        return EnterOutcome.Entered;
    }

    public async Task<bool> SelectPerson(Entity entity, int person)
    {
        var session = ecs.Get<ServerSessionComponent>(entity);
        var account = ecs.Get<ClaimsComponent>(entity);

        var personList = ecs.Get<GatePersonComponent>(entity);

        var slot = Array.FindIndex(
            personList.SlotList,
            x => Entity.Null != x && ecs.Get<ActorComponent>(x).Identifier == person);

        if (slot < 0)
        {
            session.Send(new CharacterSelectResponse { Zone = EnterMapResultValue.Failed(EnterMapResultValue.CannotEnter) });
            return false;
        }

        var worldComponent = ecs.Get<WorldComponent>(personList.SlotList[slot]);

        if (await GetDistrictAsync(worldComponent.Location) is not { } placement)
        {
            session.Send(new CharacterSelectResponse { Zone = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) });
            return false;
        }

        var (district, channel) = placement;

        var reserve = new DistrictReserveCache
        {
            Account = account.Account,
            Person = person,
            Channel = channel.Guid,
            District = district.Guid,
            X = worldComponent.Position.X,
            Y = worldComponent.Position.Y,
            Z = worldComponent.Position.Z,
            R = worldComponent.Rotation,
            Jump = worldComponent.Jump
        };

        await districtReserveCache
            .InsertAsync(reserve, ReservationLifetime);

        session.Send(new CharacterSelectResponse
        {
            Zone = new EnterMapResultValue
            {
                Zone = new ZoneValue
                {
                    Actor = person,
                    Account = account.Account,
                    World = new WorldValue
                    {
                        Location = worldComponent.Location,
                        Map = new MapValue
                        {
                            Location = worldComponent.Location,
                            Channel = channel.Identifier,
                            Server = Gate
                        },
                        Position = worldComponent.Position,
                        Rotation = worldComponent.Rotation
                    },
                    Jump = worldComponent.Jump,
                    Address = district.Host,
                    Port = district.Port,
                    Type = EnterMapType.Normal,
                    Map = new MapValue
                    {
                        Location = worldComponent.Location,
                        Channel = channel.Identifier,
                        Server = Gate
                    }
                },
                ChangeServer = true,
                ChangeType = ChangeServerType.EnterDistrict
            }
        });

        return true;
    }

    /// <summary>
    /// The district of the location with the fewest players and the emptiest of its channels, or null
    /// when no district of the location is running or none of them has a channel yet.
    /// </summary>
    public async Task<DistrictPlacement?> GetDistrictAsync(int location)
    {
        // Both searches by the location at once: one round trip instead of two after each other. A channel whose district
        // is not among the districts is left out by Pick.
        var districtsOfLocation = districts
            .Where(x => x.Location == location)
            .ToArrayAsync();

        var channelsOfLocation = channels
            .Where(c => c.Location == location)
            .ToArrayAsync();

        var availableDistricts = await districtsOfLocation;
        var allChannels = await channelsOfLocation;

        if (availableDistricts.Length == 0)
        {
            LogNoDistrict(location);

            return null;
        }

        var placement = Pick(availableDistricts, allChannels);

        if (placement is null)
        {
            LogNoChannel(location);
        }

        return placement;
    }

    /// <summary>
    /// The district with the fewest players, then with the most channels, then with the lowest workload,
    /// and the emptiest channel of it. Districts without a channel cannot take a player.
    /// </summary>
    public static DistrictPlacement? Pick(IReadOnlyCollection<DistrictCache> availableDistricts, IReadOnlyCollection<ChannelCache> allChannels)
    {
        var best = allChannels
            .GroupBy(c => c.Owner)
            .Select(g => new
            {
                District = availableDistricts.FirstOrDefault(d => d.Guid == g.Key),
                TotalOnlineCount = g.Sum(c => c.OnlineCount),
                ChannelCount = g.Count(),
                AverageWorkload = g.Average(c => (int)c.Workload),
                Channels = g.ToArray()
            })
            .Where(d => d.District is not null)
            .OrderBy(d => d.TotalOnlineCount)
            .ThenByDescending(d => d.ChannelCount)
            .ThenBy(d => d.AverageWorkload)
            .FirstOrDefault();

        if (best is null)
        {
            return null;
        }

        var bestChannel = best.Channels
            .OrderBy(c => c.OnlineCount)
            .ThenBy(c => c.Workload)
            .First();

        return new DistrictPlacement(best.District!, bestChannel);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No batch for location {Location}")]
    private partial void LogUnknownLocation(int location);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The batch of location {Location} has neither the start point {Jump} nor the default one")]
    private partial void LogNoStart(int location, int jump);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No district of location {Location} is running")]
    private partial void LogNoDistrict(int location);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The districts of location {Location} have no channel to take a player")]
    private partial void LogNoChannel(int location);
}
