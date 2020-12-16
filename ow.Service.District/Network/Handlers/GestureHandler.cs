using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        public static void GetOthers(GameSession session, DoRequest request) => session.Entity.Get<DimensionEntity>()
            .BroadcastGestureDo(session, request);

        [Handler(ServerOpcode.GestureUpdateSlots, HandlerPermission.Authorized)]
        public static void UpdateSlots(GameSession session, SlotsUpdateRequest request)
        {
            Gestures gestures = session.Entity.Get<Gestures>();

            foreach (SlotsUpdateSlotRequest gesture in request.Values)
                gestures[gesture.Id] = gesture.Value;

            EntityCharacter character = session.Entity.Get<EntityCharacter>();

            using CharacterContext context = new();
            context.UseAndSave(c => c.Update(new { character.Id, Gestures = gestures.ToArray() }));
        }
    }
}