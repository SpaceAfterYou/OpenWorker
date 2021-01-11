using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework;
using ow.Service.District.Network.Sync;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static ow.Service.District.Game.Repositories.ChannelRepository;

namespace ow.Service.District.Game.Repositories
{
    public sealed partial class ChannelRepository : Dictionary<ushort, ChannelEntity>
    {
        private readonly ConcurrentDictionary<int, ushort> _reserved = new();

        public ChannelRepository(IConfiguration configuration, Instance instance, ILogger<ChannelEntity> logger) :
            base(GetChannels(configuration, configuration["World"], configuration["District"], instance, GetChannelsCount(configuration), logger))
        {
        }

        public bool TryReserve(int character) =>
            Values.Any(channel => channel.PossibleJoin && _reserved.TryAdd(character, channel.Id));

        public bool TryJoin(SyncSession session)
        {
            if (_reserved.TryRemove(session.Character.Id, out ushort id) && TryGetValue(id, out ChannelEntity? channel))
                return channel.HardJoin(session);

            return this.Any(channel => channel.Value.TryJoin(session));
        }

        private static byte GetChannelsCount(IConfiguration configuration) =>
            byte.Parse(configuration["World:Channel:PerDistrictInstance"]);

        private static byte GetOffset(IConfiguration configuration, string world, string id, Instance instance) => configuration
            .GetSection($"World:Instance:{world}")
            .Get<InstanceConfiguration>().District
            .Where(s => s.Value.Location == instance.Location.Id)
            .Select((s, i) => new { Index = (byte)i, Value = s })
            .First(s => s.Value.Key == id)
            .Index;

        private static Dictionary<ushort, ChannelEntity> GetChannels(IConfiguration configuration, string world, string id, Instance instance, byte channels, ILogger<ChannelEntity> logger) => Enumerable
            .Range(GetOffset(configuration, world, id, instance) * channels, channels)
            .ToDictionary(k => (ushort)k, v => new ChannelEntity((ushort)v, instance, logger));
    }
}

// https://youtu.be/7hEefkR7NHc