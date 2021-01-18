using SoulCore.Game;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Network.Sync;
using System.Linq;
using static ow.Service.District.Game.Repositories.ChannelRepository;

namespace ow.Service.District.Game
{
    public sealed class ChannelMember : BaseChannelMember<ChannelEntity, SyncSession>
    {
        public void Leave() => Channel.Leave(Session);

        public void SendOtherCharactersAsync(Instance instance) =>
            SendDeferred(new CharacterOthersResponse()
            {
                Values = Channel.Sessions.Select(s => new CharacterOthersResponse.Entity()
                {
                    Character = ResponseHelper.GetCharacter(s.Value),
                    Place = ResponseHelper.GetPlace(s.Value, instance)
                })
            });

        public void SendAsync(CharacterSpecialOptionListUpdateRequest request)
        {
            SyncSession? session = Channel.Sessions.Values.FirstOrDefault(s => s.Character.Id == request.Character);
            if (session is null)
                return;

            SendDeferred(new CharacterSpecialOptionListUpdateResponse()
            {
                Character = session.Character.Id,
                Values = session.SpecialOptions.Select(s => new CharacterSpecialOptionListUpdateResponse.Entity()
                {
                    Id = s.Id,
                    Value = s.Value
                })
            });
        }

        public ChannelMember(SyncSession session, ChannelEntity channel) : base(session, channel)
        {
        }
    }
}

// https://youtu.be/7mosZiponDg
