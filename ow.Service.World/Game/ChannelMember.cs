using ow.Service.District.Game.Helpers;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Sync;
using SoulCore.IO.Network;
using SoulCore.IO.Network.Requests.Character;
using SoulCore.IO.Network.Responses;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class ChannelMember : BaseChannelMember<ChannelRepository.Channel, SyncServer, SyncSession>
    {
        internal static readonly ChannelMember Empty = new();

        public void Leave() => Channel.Leave(Session);

        public void SendOtherCharactersDeferred(Instance instance) =>
            SendAsync(new CharacterOthersResponse
            {
                Values = Channel.Sessions.Select(s => new CharacterOthersResponse.Entity()
                {
                    Character = ResponseHelper.GetCharacterExPacket(s.Value),
                    Place = ResponseHelper.GetPlace(s.Value, instance)
                })
            });

        public void SendAsync(CharacterUpdateSpecialOptionListRequest request)
        {
            SyncSession? session = Channel.Sessions.Values.FirstOrDefault(s => s.Character.Id == request.Character);
            if (session is null)
            {
                return;
            }

            SendAsync(new CharacterSpecialOptionListUpdateResponse
            {
                Character = session.Character.Id,
                Values = session.SpecialOptions.Select(s => new CharacterSpecialOptionListUpdateResponse.Entity
                {
                    Id = s.Id,
                    Value = s.Value
                })
            });
        }

        public ChannelMember(SyncSession session, ChannelRepository.Channel channel) : base(session, channel)
        {
        }

        private ChannelMember() : base(SyncSession.Empty, ChannelRepository.Channel.Empty)
        {
        }
    }
}

// https://youtu.be/7mosZiponDg