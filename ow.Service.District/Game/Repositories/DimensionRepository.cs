using Microsoft.Extensions.Configuration;
using ow.Framework;
using ow.Framework.Game;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Network;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    internal class DimensionRepository : Dictionary<ushort, DimensionRepository.Entity>
    {
        internal sealed class Entity : BaseDimension
        {
            private readonly object _joinLock = new();
            private readonly Instance _instance;

            internal bool TryJoin(Session session)
            {
                lock (_joinLock)
                {
                    if (Sessions.Count >= Defines.MaxChannelSessions || !Join(session))
                        return false;
                }

                session.Dimension = new(session, this);

                BroadcastAsync(session, new DimensionBrodcastCharacterInResponse()
                {
                    Character = ResponseHelper.GetCharacter(session),
                    Place = ResponseHelper.GetPlace(session, _instance)
                });

                return true;
            }

            internal void Leave(Session session)
            {
                if (base.Leave(session))
                    BroadcastAsync(session, new DimensionBrodcastCharacterOutResponse()
                    {
                        Id = session.Character.Id
                    });
            }

            internal Entity(ushort id, Instance instance) : base(id) => _instance = instance;
        }

        public DimensionRepository(IConfiguration configuration, Instance instance) : base(GetDimensions(configuration, instance))
        {
        }

        internal bool Join(Session session) => this.Any(dimension => dimension.Value.TryJoin(session));

        private static Dictionary<ushort, Entity> GetDimensions(IConfiguration configuration, Instance instance) => Enumerable
            .Range(byte.Parse(configuration[$"Districts:{configuration["Id"]}:DimensionsOffset"]), byte.Parse(configuration["DistrictsDimensionsPerInstance"]))
            .ToDictionary(k => (ushort)k, v => new Entity((ushort)v, instance));
    }
}

// https://youtu.be/7mosZiponDg
// https://youtu.be/7hEefkR7NHc
