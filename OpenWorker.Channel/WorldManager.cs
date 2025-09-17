using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Persons.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.UpdateContent.Res.Rows;
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
    private short Gate { get; } = configuration.GetGate();

    public async ValueTask<bool> TryEnter(Entity player, short location)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type))
        {
            return false;
        }

        switch (type)
        {
            case BatchType.Maze:
                await EnterMaze(player, location, batch).ConfigureAwait(false);
                return true;
            
            case BatchType.District:
                await TryEnterDistrict(player, location, batch).ConfigureAwait(false);
                return true;

            case BatchType.None:
            default:
                return false;
        }
    }

    public async ValueTask<bool> TryEnterMaze(Entity player, short location)
    {
        if (!provider.TryGetAndCache(location, out var batch, out var type) || type != BatchType.Maze)
        {
            return false;
        }
        
        await EnterMaze(player, location, batch).ConfigureAwait(false);
        return true;
    }
    
    private async ValueTask EnterMaze(Entity player, short location, VBatchFile batch)
    {
        var session = ecs.Get<ServerSessionComponent>(player);

        var jump = location * 100 + 1;
        var start = batch.EventBox.StartEvents.First(x => x.Id == jump);
        
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
                Account = ecs.Get<ClaimsComponent>(player),
                World = world,
                Jump = start.Id,
                Address = "127.0.0.1",
                Port = 10101,
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
            Person = ecs.Get<ActorComponent>(player),
            X = world.Position.X,
            Y = world.Position.Y,
            Z = world.Position.Z,
            R = world.Rotation,
            Jump = start.Id
        };

        await mazeReserves
            .InsertAsync(cache)
            .ConfigureAwait(false);

        session.Send(new WorldEnterResponse(enter));
    }
    
    private async ValueTask TryEnterDistrict(Entity player, short location, VBatchFile batch)
    {
        var session = ecs.Get<ServerSessionComponent>(player);

        var jump = location * 100 + 1;
        var start = batch.EventBox.StartEvents.First(x => x.Id == jump);
        
        var (district, channel) = await GetDistrictAsync(location).ConfigureAwait(false);

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
                Account = ecs.Get<ClaimsComponent>(player),
                World = world,
                Jump = start.Id,
                Address = district.Host,
                Port = district.Port,
                Type = EnterMapType.Maze,
                Map = map
            },
            ChangeServer = true,
            ChangeType = ChangeServerType.EnterDistrict
        };

        var cache = new MazeReserveCache
        {
            Account = ecs.Get<ClaimsComponent>(player),
            Location = location,
            Person = ecs.Get<ActorComponent>(player),
            X = world.Position.X,
            Y = world.Position.Y,
            Z = world.Position.Z,
            R = world.Rotation,
            Jump = start.Id
        };

        await mazeReserves
            .InsertAsync(cache)
            .ConfigureAwait(false);

        session.Send(new WorldEnterResponse(enter));
    }
    
    public async Task SelectPerson(Entity entity, int person)
    {
        var session = ecs.Get<ServerSessionComponent>(entity);
        var account = ecs.Get<ClaimsComponent>(entity);
        
        var personList = ecs.Get<GatePersonComponent>(entity);
        
        var personComponent = personList.SlotList.First(x => Entity.Null != x && ecs.Get<ActorComponent>(x).Identifier == person);
        
        var worldComponent = ecs.Get<WorldComponent>(personComponent);
        
        var (district, channel) = await GetDistrictAsync(worldComponent.Location).ConfigureAwait(false);

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
            .InsertAsync(reserve, WhenKey.NotExists)
            .ConfigureAwait(false);
        
        session.Send(new CharacterSelectResponse(
            new EnterMapResultValue
            {
                Zone = new ZoneValue
                {
                    Actor = person,
                    Account = account.Account,
                    World = new WorldValue(worldComponent)
                    {
                        Map = new MapValue
                        {
                            Location = worldComponent.Location,
                            Channel = channel.Identifier,
                            Server = Gate
                        }
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
            }));
    }

    public async Task<Tuple<DistrictCache, ChannelCache>> GetDistrictAsync(int location)
    {
        var availableDistricts = await districts
            .Where(x => x.Location == location)
            .ToArrayAsync()
            .ConfigureAwait(false);

        var districtGuids = availableDistricts
            .Select(d => d.Guid)
            .ToArray();

        var allChannels = await channels
            .Where(c => districtGuids.Contains(c.Owner))
            .ToArrayAsync()
            .ConfigureAwait(false);

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