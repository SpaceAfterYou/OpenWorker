using ow.Framework.Game.Character;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Framework.IO.Network.Requests.Movement;

namespace ow.Framework.Game.Entities
{
    public sealed class DimensionMemberEntity
    {
        private readonly DimensionEntity _dimension;
        private readonly GameSession _session;

        public void Leave() => _dimension.Leave(_session);

        #region Broadcast Channel

        public void BroadcastChatMessage(in ReceiveRequest request) =>
            BroadcastChatMessage(request.Type, request.Message);

        public void BroadcastChatMessage(ChatType type, string message)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            _dimension.BroadcastAsync(writer);
        }

        #endregion Broadcast Channel

        #region Broadcast Character

        public void BroadcastCharacterSetLevel(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);
            writer.Write(character.Level);

            _dimension.BroadcastAsync(writer);
        }

        public void BroadcastCharacterToggleWeapon(in ToggleWeaponRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            _dimension.BroadcastAsync(writer);
        }

        #endregion Broadcast Character

        #region Broadcast Gesture

        public void BroadcastGestureDo(in DoRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.GestureDo);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.GestureId);
            writer.WriteVector3(request.Position);
            writer.Write(request.Unknown1);
            writer.Write(request.Rotation);

            _dimension.BroadcastAsync(writer);
        }

        #endregion Broadcast Gesture

        #region Broadcast Storage

        //public void BroadcastItemUpgradeResponse(BaseItem item)
        //{ }

        //public void BroadcastItemMove(params BaseItem[] slots)
        //{
        //    // var size
        //    //     = sizeof(uint) /* Count */
        //    //     + (SystemDefinition.ItemSize * slots.Length) /* Items */
        //    //     + sizeof(byte) /* Unknown */;
        //    //
        //    // using PacketWriter writer = new(size, ClientOpcode.StorageItemMoveBroadcast);
        //    //
        //    // writer.Write(slots.Length);
        //    // foreach (var item in slots)
        //    // {
        //    //     writer.Write(item);
        //    // }
        //    //
        //    // writer.Write((byte)0);
        //    //
        //    // SendExceptAsync(session, writer);
        //}

        #endregion Broadcast Storage

        #region Broadcast Movement

        public void BroadcastMovementMove(in MoveRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.Unknown1);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.WriteVector2(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);
            writer.Write(request.Unknown7);
            writer.Write(request.Unknown8);

            _dimension.BroadcastAsync(writer);
        }

        public void BroadcastMovementStop(in StopRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.Unknown1);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Unknown4);
            writer.Write(request.Unknown5);

            _dimension.BroadcastAsync(writer);
        }

        public void BroadcastMovementJump(in JumpRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            EntityCharacter character = _session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.Unknown1);
            writer.Write(request.Unknown2);
            writer.Write(request.Location);
            writer.Write(request.Unknown3);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.WriteVector2(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);

            _dimension.BroadcastAsync(writer);
        }

        public void BroadcastLoopMotionEnd(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            _dimension.BroadcastAsync(writer);
        }

        #endregion Broadcast Movement

        internal DimensionMemberEntity(GameSession session, DimensionEntity dimension) => (_session, _dimension) = (session, dimension);
    }
}

// https://youtu.be/7mosZiponDg