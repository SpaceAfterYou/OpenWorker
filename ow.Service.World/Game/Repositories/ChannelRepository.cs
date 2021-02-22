using Microsoft.Extensions.Configuration;
using ow.Framework.Configuration;
using ow.Service.District.Network.Sync;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static ow.Service.District.Game.Repositories.ChannelRepository;

namespace ow.Service.District.Game.Repositories
{
    public sealed partial class ChannelRepository : Dictionary<ushort, Channel>
    {
        internal static readonly ChannelRepository Empty = new();

        private readonly ConcurrentDictionary<int, ushort> _reserved = new();

        public ChannelRepository(IConfiguration configuration, Instance instance) :
            base(GetChannels(configuration, configuration["World"], configuration["District"], instance, GetChannelsCount(configuration)))
        {
        }

        public bool TryReserve(int character) => Values.Any(channel => channel.PossibleJoin && _reserved.TryAdd(character, channel.Id));

        public bool TryJoin(SyncSession session)
        {
            if (_reserved.TryRemove(session.Character.Id, out ushort id) && TryGetValue(id, out Channel? channel))
            {
                return channel.HardJoin(session);
            }

            return this.Any(channel => channel.Value.TryJoin(session));
        }

        private ChannelRepository() : base(ImmutableDictionary<ushort, Channel>.Empty)
        {
        }

        private static byte GetChannelsCount(IConfiguration configuration) =>
            byte.Parse(configuration["World:Channel:PerDistrictInstance"]);

        private static byte GetOffset(IConfiguration configuration, string world, string id, Instance instance) => configuration
            .GetSection($"World:Instance:{world}")
            .Get<InstanceConfiguration>().District
            .Where(s => s.Value.Location == instance.LocationId)
            .Select((s, i) => new { Index = (byte)i, Value = s })
            .First(s => s.Value.Key == id)
            .Index;

        private static Dictionary<ushort, Channel> GetChannels(IConfiguration configuration, string world, string id, Instance instance, byte channels) => Enumerable
            .Range(GetOffset(configuration, world, id, instance) * channels, channels)
            .ToDictionary(k => (ushort)k, v => new Channel((ushort)v, instance));
    }
}

// https://youtu.be/7hEefkR7NHc