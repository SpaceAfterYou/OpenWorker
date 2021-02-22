using ow.Service.District.Game.Helpers;
using ow.Service.District.Network.Sync;
using SoulCore.IO.Network;
using SoulCore.IO.Network.Responses;
using SoulCore.Types;

namespace ow.Service.District.Game.Repositories
{
    public sealed partial class ChannelRepository
    {
        public sealed class Channel : BaseChannel<SyncServer, SyncSession>
        {
            internal static readonly Channel Empty = new();

            public const byte MaxChannelSessions = 96;

            public bool PossibleJoin => Sessions.Count < MaxChannelSessions;

            public ChannelLoadStatus Status => Sessions.Count switch
            {
                > 64 => ChannelLoadStatus.Full,
                > 48 => ChannelLoadStatus.High,
                > 32 => ChannelLoadStatus.Medium,
                _ => ChannelLoadStatus.Low
            };

            private readonly object _joinLock = new();
            private readonly Instance _instance = Instance.Empty;

            public bool TryJoin(SyncSession session)
            {
                lock (_joinLock)
                {
                    if (PossibleJoin || !TryAdd(session))
                    {
                        return false;
                    }
                }

                return Join(session);
            }

            public bool HardJoin(SyncSession session) => TryAdd(session) && Join(session);

            public void Leave(SyncSession session)
            {
                if (TryRemove(session, out _))
                {
                    BroadcastDeferred(session, new WorldOutInfoPcResponse
                    {
                        CharacterId = session.Character.Id
                    });
                }
            }

            public Channel(ushort id, Instance instance) : base(id) => _instance = instance;

            private Channel() : base(0)
            {
            }

            private bool Join(SyncSession session)
            {
                session.Channel = new(session, this);

                BroadcastDeferred(session, new WorldInInfoPcResponse
                {
                    Character = ResponseHelper.GetCharacterExPacket(session),
                    Place = ResponseHelper.GetPlace(session, _instance)
                });

                return true;
            }
        }
    }
}

// https://youtu.be/7hEefkR7NHc