using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Channel;

public sealed partial class WorldManager(
    IConfiguration configuration,
    BatchManager provider,
    IRedisCollection<DistrictReserveCache> districtReserveCache,
    World ecs,
    IRedisCollection<ChannelCache> channels,
    IRedisCollection<DistrictCache> districts,
    IRedisCollection<MazeReserveCache> mazeReserves,
    IRedisCollection<MazeCache> mazes,
    ILogger<WorldManager> logger)
{
    /// <summary>
    /// How long a move between servers stays valid. The client reconnects right after the response,
    /// so an expired reservation means that it never came.
    /// </summary>
    public static TimeSpan ReservationLifetime { get; } = TimeSpan.FromSeconds(60);

    private short Gate { get; } = configuration.GetGate();
    /// <summary>
    /// Where players go when no maze server of the gate has registered itself.
    /// </summary>
    private (string Host, short Port) DefaultMaze { get; } = configuration.GetMazeAddress();

    /// <summary>
    /// The maze server of this gate with the fewest players.
    /// </summary>
    private async ValueTask<(string Host, short Port)> GetMazeAsync()
    {
        int gate = Gate;

        var running = await mazes
            .Where(x => x.Gate == gate)
            .ToListAsync();

        return running.MinBy(x => x.Players) is { } maze ? (maze.Host, maze.Port) : DefaultMaze;
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
                await EnterMaze(player, location, start);
                return EnterOutcome.Entered;

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

        await EnterMaze(player, location, start);
        return true;
    }

    /// <summary>
    /// Finds the requested start point, or the default one of the location when none was requested
    /// or the requested one does not exist.
    /// </summary>
    private static bool TryGetStart(VBatchFile batch, short location, int jump, out StartEventBox start)
    {
        var fallback = location * 100 + 1;
        var found = false;

        start = default!;

        foreach (var box in batch.EventBox.StartEvents)
        {
            if (jump != 0 && box.Id == jump)
            {
                start = box;
                return true;
            }

            if (box.Id == fallback)
            {
                start = box;
                found = true;
            }
        }

        return found;
    }

    private async ValueTask EnterMaze(Entity player, short location, StartEventBox start)
    {
        var session = ecs.Get<ServerSessionComponent>(player);
        var maze = await GetMazeAsync();

        // The player may have left while the servers were looked up.
        if (!ecs.IsAlive(player))
        {
            return;
        }

        var map = new MapValue
        {
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
            X = world.Position.X,
            Y = world.Position.Y,
            Z = world.Position.Z,
            R = world.Rotation,
            Jump = start.Id
        };

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
        var availableDistricts = await districts
            .Where(x => x.Location == location)
            .ToArrayAsync();

        if (availableDistricts.Length == 0)
        {
            LogNoDistrict(location);

            return null;
        }

        var districtGuids = availableDistricts
            .Select(d => d.Guid)
            .ToArray();

        var allChannels = await channels
            .Where(c => districtGuids.Contains(c.Owner))
            .ToArrayAsync();

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
