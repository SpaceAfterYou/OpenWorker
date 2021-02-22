using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Utils;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Requests.Gesture;
using SoulCore.IO.Network.Responses.Gesture;
using SoulCore.Types;
using System.Linq;
using System.Threading.Tasks;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class GestureHandler
    {
        [Handler(CategoryCommand.Gesture, GestureCommand.Show)]
        public static void OnShow(SyncSession session, GestureShowRequest request)
        {
            if (!session.Server.Table.Gesture.ContainsKey((ushort)request.GestureId))
            {
                NetworkUtils.Drop(session, $"#{request.GestureId} gesture not found");
                return;
            }

            session.Channel.BroadcastDeferred(new GestureShowResponse()
            {
                CharacterId = session.Character.Id,
                GestureId = request.GestureId,
                Position = request.Position,
                Yaw = request.Yaw,
                Pitch = request.Pitch,
            });
        }

        [Handler(CategoryCommand.Gesture, GestureCommand.SlotUpdate)]
        public static void OnSlotUpdate(SyncSession session, GestureSlotUpdateRequest request)
        {
            foreach (uint gestureId in request.GestureIds)
            {
                if (gestureId == 0)
                {
                    continue;
                }

                if (!session.Server.Table.Gesture.TryGetValue((ushort)gestureId, out GestureEntity? gesture))
                {
                    NetworkUtils.Drop(session, $"#{gestureId} gesture not found");
                    return;
                }

                if (gesture!.Class != session.Character.Class && gesture!.Class != Class.None)
                {
                    NetworkUtils.Drop(session, $"#{gestureId} bad gesture");
                    return;
                }
            }

            _ = Task.Run(async () =>
              {
                  await using CharacterContext context = session.Server.DatabaseCharacter.CreateDbContext();

                  CharacterModel? model = await context.Characters.FirstOrDefaultAsync(s => s.Id == session.Character.Id).ConfigureAwait(false);
                  if (model is null)
                  {
                      session.Disconnect();
                  }
                  else
                  {
                      model.Gestures = request.GestureIds.ToArray();
                      context.Update(model);

                      await context.SaveChangesAsync().ConfigureAwait(false);
                  }
              });

            session.SendAsync(new CharacterGestureUpdateSlotsResponse() { Values = request.Values });
        }
    }
}