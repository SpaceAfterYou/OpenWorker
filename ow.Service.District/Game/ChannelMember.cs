using ow.Framework.Game;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Sync;
using System.Linq;

namespace ow.Service.District.Game
{
    internal sealed class ChannelMember : BaseChannelMember<ChannelRepository.Entity, SyncSession>
    {
        internal void Leave() => Channel.Leave(Session);

        internal void SendOtherCharactersAsync(Instance instance) =>
            SendAsync(new CharacterOthersResponse()
            {
                Values = Channel.Sessions.Select(s => new CharacterOthersResponse.Entity()
                {
                    Character = ResponseHelper.GetCharacter(s.Value),
                    Place = ResponseHelper.GetPlace(s.Value, instance)
                })
            });

        internal void SendAsync(CharacterSpecialOptionListUpdateRequest request)
        {
            SyncSession? session = Channel.Sessions.Values.FirstOrDefault(s => s.Character.Id == request.Character);
            if (session is null)
                return;

            SendAsync(new CharacterSpecialOptionListUpdateResponse()
            {
                Character = session.Character.Id,
                Values = session.SpecialOptions.Select(s => new CharacterSpecialOptionListUpdateResponse.Entity()
                {
                    Id = s.Id,
                    Value = s.Value
                })
            });
        }

        internal ChannelMember(SyncSession session, ChannelRepository.Entity channel) : base(session, channel)
        {
        }
    }
}

// https://youtu.be/7mosZiponDg