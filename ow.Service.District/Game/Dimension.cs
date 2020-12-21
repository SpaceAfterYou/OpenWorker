using ow.Framework;
using ow.Framework.Game;
using ow.Framework.IO.Network.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Network;

namespace ow.Service.District.Game
{
    internal sealed class Dimension : BaseDimension
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

        internal Dimension(ushort id, Instance instance) : base(id) => _instance = instance;
    }
}

// https://youtu.be/7mosZiponDg
// https://youtu.be/7hEefkR7NHc