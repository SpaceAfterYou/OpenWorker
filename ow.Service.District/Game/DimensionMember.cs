using ow.Framework.Game;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Sync;
using System.Linq;

namespace ow.Service.District.Game
{
    internal sealed class DimensionMember : BaseDimensionMember<DimensionRepository.Entity, Session>
    {
        internal void Leave() => Dimension.Leave(Session);

        internal void SendOtherCharactersAsync(Instance instance) =>
            SendAsync(new CharacterOthersResponse()
            {
                Values = Dimension.Sessions.Select(s => new CharacterOthersResponse.Entity()
                {
                    Character = ResponseHelper.GetCharacter(s.Value),
                    Place = ResponseHelper.GetPlace(s.Value, instance)
                })
            });

        internal void SendAsync(CharacterSpecialOptionListUpdateRequest request)
        {
            Session? session = Dimension.Sessions.Values.FirstOrDefault(s => s.Character.Id == request.Character);
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

        internal DimensionMember(Session session, DimensionRepository.Entity dimension) : base(session, dimension)
        {
        }
    }
}

// https://youtu.be/7mosZiponDg