using ow.Framework.Database.Characters;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        internal static void GetOthers(Session session, GestureDoRequest request) => session.Dimension
            .BroadcastAsync(request);

        [Handler(ServerOpcode.GestureUpdateSlots, HandlerPermission.Authorized)]
        internal static void UpdateSlots(Session session, GestureQuickSlotsUpdateRequest request, BinTables tables)
        {
            foreach (uint id in request.Values)
                if (id != 0 && !tables.Gesture.ContainsKey((ushort)id))
                    NetworkUtils.DropSession();

            using CharacterContext context = new();
            context.UseAndSave(c => c.Update(new { session.Character.Id, Gestures = request.Values }));

            session.SendAsync(new GestureUpdateSlotsResponse()
            {
                Values = request.Values
            });
        }
    }
}