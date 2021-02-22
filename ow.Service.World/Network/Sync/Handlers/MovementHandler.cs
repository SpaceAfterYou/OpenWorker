using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Requests.Move;
using SoulCore.IO.Network.Responses;

namespace ow.Service.District.Network.Sync.Handlers
{
    public static class MovementHandler
    {
        [Handler(CategoryCommand.Move, MoveCommand.Jump)]
        public static void Jump(SyncSession session, MoveJumpRequest request)
        {
            session.Character.Place.Position = request.Position;
            session.Character.Place.Yaw = request.Yaw;

            session.Channel.BroadcastDeferred(request);
        }

        [Handler(CategoryCommand.Move, MoveCommand.LoopMotionEnd)]
        public static void LoopMotionEndBroadcast(SyncSession session) => session.Channel
            .BroadcastDeferred(new CharacterLoopMotionEndResponse() { Character = session.Character.Id });

        [Handler(CategoryCommand.Move, MoveCommand.Move)]
        public static void Move(SyncSession session, MoveMoveRequest request)
        {
            session.Character.Place.Position = request.Position;
            session.Character.Place.Yaw = request.Yaw;

            session.Channel.BroadcastDeferred(request);
        }

        [Handler(CategoryCommand.Move, MoveCommand.StopBt)]
        public static void Stop(SyncSession session, MoveStopRequest request)
        {
            session.Character.Place.Position = request.Position;
            session.Character.Place.Yaw = request.Yaw;

            session.Channel.BroadcastDeferred(request);
        }
    }
}