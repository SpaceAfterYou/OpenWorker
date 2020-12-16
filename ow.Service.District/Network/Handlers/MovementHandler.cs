using ow.Framework.Game.Datas;
using ow.Framework.Game.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Movement;

namespace ow.Service.District.Network.Handlers
{
    internal static class MovementHandler
    {
        [Handler(ServerOpcode.MovementJump, HandlerPermission.Authorized)]
        public static void Jump(GameSession session, JumpRequest request)
        {
            Place place = session.Entity.Get<Place>();
            place.Position = request.Position;
            place.Rotation = request.Rotation;

            session.Entity.Get<DimensionMemberEntity>().BroadcastMovementJump(request);
        }

        [Handler(ServerOpcode.MovementLoopMotionEnd, HandlerPermission.Authorized)]
        public static void LoopMotionEndBroadcast(GameSession session) =>
            session.Entity.Get<DimensionMemberEntity>().BroadcastLoopMotionEnd(session);

        [Handler(ServerOpcode.MovementMove, HandlerPermission.Authorized)]
        public static void Move(GameSession session, MoveRequest request)
        {
            Place place = session.Entity.Get<Place>();
            place.Position = request.Position;
            place.Rotation = request.Rotation;

            session.Entity.Get<DimensionMemberEntity>().BroadcastMovementMove(request);
        }

        [Handler(ServerOpcode.MovementStopBt, HandlerPermission.Authorized)]
        public static void Stop(GameSession session, StopRequest request)
        {
            Place place = session.Entity.Get<Place>();
            place.Position = request.Position;
            place.Rotation = request.Rotation;

            session.Entity.Get<DimensionMemberEntity>().BroadcastMovementStop(request);
        }
    }
}