using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Commands.Old;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        public static void GetOthers(SyncSession session, GestureDoRequest request) => session.Channel?
            .BroadcastDeferred(new CharacterGestureDo()
            {
                Character = session.Character.Id,
                Gesture = request.Gesture,
                Position = request.Position,
                Rotation = request.Rotation
            });

        [Handler(ServerOpcode.GestureUpdateSlots, HandlerPermission.Authorized)]
        public void UpdateSlots(SyncSession session, GestureQuickSlotsUpdateRequest request)
        {
            foreach (uint id in request.Values)
            {
                if (id == 0)
                    continue;

                if (!_tables.Gesture.TryGetValue((ushort)id, out GestureTableEntity? gesture))
                    NetworkUtils.DropBadAction();

                if (gesture!.Hero != session.Character.Hero && gesture!.Hero != Hero.None)
                    NetworkUtils.DropBadAction();
            }

            using CharacterContext context = _characterRepository.CreateDbContext();

            CharacterModel model = context.Characters.First(s => s.Id == session.Character.Id);
            model.Gestures = request.Values.ToArray();

            context.UseAndSave(c => c.Update(model));

            session.SendDeferred(new CharacterGestureUpdateSlotsResponse() { Values = request.Values });
        }

        public GestureHandler(BinTables tables, IDbContextFactory<CharacterContext> characterRepository) =>
            (_tables, _characterRepository) = (tables, characterRepository);

        private readonly BinTables _tables;
        private readonly IDbContextFactory<CharacterContext> _characterRepository;
    }
}