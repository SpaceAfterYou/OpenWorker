using ow.Framework;
using ow.Framework.Game;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Network.Sync;

namespace ow.Service.District.Game.Repositories
{
    public sealed partial class ChannelRepository
    {
        public sealed class ChannelEntity : BaseChannel<SyncSession>
        {
            public bool PossibleJoin => Sessions.Count < Defines.MaxChannelSessions;

            public ChannelLoadStatus Status => Sessions.Count switch
            {
                > 64 => ChannelLoadStatus.Full,
                > 48 => ChannelLoadStatus.High,
                > 32 => ChannelLoadStatus.Medium,
                _ => ChannelLoadStatus.Low
            };

            private readonly object _joinLock = new();
            private readonly Instance _instance;

            public bool TryJoin(SyncSession session)
            {
                lock (_joinLock)
                    if (PossibleJoin || !TryAdd(session))
                        return false;

                return Join(session);
            }

            public bool HardJoin(SyncSession session) =>
                TryAdd(session) && Join(session);

            public void Leave(SyncSession session)
            {
                if (TryRemove(session, out _))
                    BroadcastDeferred(session, new SChannelBroadcastCharacterOutResponse
                    {
                        Id = session.Character.Id
                    });
            }

            public ChannelEntity(ushort id, Instance instance) : base(id) =>
                _instance = instance;

            private bool Join(SyncSession session)
            {
                session.Channel = new(session, this);

                BroadcastDeferred(session, new SChannelBroadcastCharacterInResponse
                {
                    Character = ResponseHelper.GetCharacter(session),
                    Place = ResponseHelper.GetPlace(session, _instance)
                });

                return true;
            }
        }
    }
}

// https://youtu.be/7hEefkR7NHc