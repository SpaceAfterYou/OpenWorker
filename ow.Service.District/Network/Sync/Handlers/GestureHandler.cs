using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        public static void GetOthers(Session session, GestureDoRequest request) => session.Dimension
            .BroadcastAsync(request);

        [Handler(ServerOpcode.GestureUpdateSlots, HandlerPermission.Authorized)]
        public void UpdateSlots(Session session, GestureQuickSlotsUpdateRequest request)
        {
            foreach (uint id in request.Values)
                if (id != 0 && !_tables.Gesture.ContainsKey((ushort)id))
                    NetworkUtils.DropSession();

            using CharacterContext context = _characterRepository.CreateDbContext();
            context.UseAndSave(c => c.Update(new { session.Character.Id, Gestures = request.Values }));

            session.SendAsync(new GestureUpdateSlotsResponse() { Values = request.Values });
        }

        public GestureHandler(BinTables tables, IDbContextFactory<CharacterContext> characterRepository) =>
            (_tables, _characterRepository) = (tables, characterRepository);

        private readonly BinTables _tables;
        private readonly IDbContextFactory<CharacterContext> _characterRepository;
    }
}