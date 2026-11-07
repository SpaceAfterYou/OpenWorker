using System.Diagnostics;
using Arch.Core;
using Microsoft.Extensions.Configuration;
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

public sealed class WorldManager(
    IConfiguration configuration,
    BatchManager provider,
    IRedisCollection<DistrictReserveCache> districtReserveCache,
    World ecs,
    IRedisCollection<ChannelCache> channels,
    IRedisCollection<DistrictCache> districts,
    IRedisCollection<MazeReserveCache> mazeReserves)
{
    /// <summary>
    /// How long a move between servers stays valid. The client reconnects right after the response,
    /// so an expired reservation means that it never came.
    /// </summary>
    public static TimeSpan ReservationLifetime { get; } = TimeSpan.FromSeconds(60);

    private short Gate { get; } = configuration.GetGate();
    private (string Host, short Port) Maze { get; } = configuration.GetMazeAddress();

    /// <summary>
    /// Enters the location or tells the client that the entry failed, so that it does not wait forever.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, short location, int jump)
    {
        if (await TryEnter(player, location, jump))
        {
            return;
        }

        ecs.Get<ServerSessionComponent>(player).Send(new WorldEnterResponse { Map = EnterMapResultValue.Error });
    }

    public async ValueTask<bool> TryEnter(Entity player, short location, int jump = 0)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type) ||
            !TryGetStart(batch, location, jump, out var start))
        {
            return false;
        }

        switch (type)
        {
            case BatchType.Maze:
                await EnterMaze(player, location, start);
                return true;

            case BatchType.District:
                int districtLocation = location;

                if (!await districts.AnyAsync(x => x.Location == districtLocation))
                {
                    return false;
                }

                await TryEnterDistrict(player, location, start);
                return true;

            case BatchType.None:
            default:
                return false;
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
                Address = Maze.Host,
                Port = Maze.Port,
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

    private async ValueTask TryEnterDistrict(Entity player, short location, StartEventBox start)
    {
        var session = ecs.Get<ServerSessionComponent>(player);

        var (district, channel) = await GetDistrictAsync(location);

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

        int location = worldComponent.Location;

        if (!await districts.AnyAsync(x => x.Location == location))
        {
            session.Send(new CharacterSelectResponse { Zone = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) });
            return false;
        }

        var (district, channel) = await GetDistrictAsync(worldComponent.Location);

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

    public async ValueTask<bool> IsDistrictRunningAsync(int location)
    {
        return await districts.AnyAsync(x => x.Location == location);
    }

    public async Task<Tuple<DistrictCache, ChannelCache>> GetDistrictAsync(int location)
    {
        var availableDistricts = await districts
            .Where(x => x.Location == location)
            .ToArrayAsync();

        var districtGuids = availableDistricts
            .Select(d => d.Guid)
            .ToArray();

        var allChannels = await channels
            .Where(c => districtGuids.Contains(c.Owner))
            .ToArrayAsync();

        Debug.Assert(allChannels.Length > 0);

        var districtChannelStats = allChannels
            .GroupBy(c => c.Owner)
            .Select(g => new
            {
                DistrictGuid = g.Key,
                TotalOnlineCount = g.Sum(c => c.OnlineCount),
                ChannelCount = g.Count(),
                AverageWorkload = g.Average(c => (int)c.Workload),
                Channels = g.ToArray()
            })
            .ToArray();

        var bestDistrict = districtChannelStats
            .OrderBy(d => d.TotalOnlineCount)
            .ThenByDescending(d => d.ChannelCount)
            .ThenBy(d => d.AverageWorkload)
            .First();

        var bestChannel = bestDistrict.Channels
            .OrderBy(c => c.OnlineCount)
            .ThenBy(c => c.Workload)
            .First();

        return Tuple.Create(availableDistricts.First(d => d.Guid == bestDistrict.DistrictGuid), bestChannel);
    }
}
