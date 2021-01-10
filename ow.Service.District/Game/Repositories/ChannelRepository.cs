using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework;
using ow.Framework.Game;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Network.Sync;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class ChannelRepository : Dictionary<ushort, ChannelRepository.Entity>
    {
        public sealed class Entity : BaseChannel<SyncSession>
        {
            private readonly object _joinLock = new();
            private readonly Instance _instance;

            internal bool TryJoin(SyncSession session)
            {
                lock (_joinLock)
                {
                    if (Sessions.Count >= Defines.MaxChannelSessions || !Join(session))
                        return false;
                }

                session.Channel = new(session, this);

                BroadcastAsync(session, new ChannelBroadcastCharacterInResponse
                {
                    Character = ResponseHelper.GetCharacter(session),
                    Place = ResponseHelper.GetPlace(session, _instance)
                });

                return true;
            }

            internal new void Leave(SyncSession session)
            {
                if (base.Leave(session))
                    BroadcastAsync(session, new ChannelBroadcastCharacterOutResponse
                    {
                        Id = session.Character.Id
                    });
            }

            internal Entity(ushort id, Instance instance, ILogger<Entity> logger) : base(id, logger) =>
                _instance = instance;
        }

        public ChannelRepository(IConfiguration configuration, Instance instance, ILogger<Entity> logger) :
            base(GetChannels(configuration, configuration["World"], configuration["District"], instance, GetChannelsCount(configuration), logger))
        {
        }

        internal bool Join(SyncSession session) => this.Any(channel => channel.Value.TryJoin(session));

        private static byte GetChannelsCount(IConfiguration configuration) =>
            byte.Parse(configuration["World:Channel:PerDistrictInstance"]);

        private static byte GetOffset(IConfiguration configuration, string world, string id, Instance instance) => configuration
            .GetSection($"World:Instance:{world}")
            .Get<InstanceConfiguration>().District
            .Where(s => s.Value.Location == instance.Location.Id)
            .Select((s, i) => new { Index = (byte)i, Value = s })
            .First(s => s.Value.Key == id)
            .Index;

        private static Dictionary<ushort, Entity> GetChannels(IConfiguration configuration, string world, string id, Instance instance, byte channels, ILogger<Entity> logger) => Enumerable
            .Range(GetOffset(configuration, world, id, instance) * channels, channels)
            .ToDictionary(k => (ushort)k, v => new Entity((ushort)v, instance, logger));
    }
}

// https://youtu.be/7hEefkR7NHc