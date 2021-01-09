using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using System.Linq;

namespace ow.Framework.Game
{
    public abstract class BaseChannelMember<TChannel, TSession>
        where TChannel : BaseChannel<TSession>
        where TSession : BaseSyncSession
    {
        protected readonly TChannel Channel;
        protected readonly TSession Session;

        public void SendAsync(CharacterSpecialOptionListUpdateResponse value) =>
            Session.SendAsync(ClientOpcode.CharacterSpecialOptionUpdateList, (PacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write((byte)value.Values.Count());

                foreach (CharacterSpecialOptionListUpdateResponse.Entity option in value.Values)
                {
                    writer.WriteSpecialOption(option.Id);
                    writer.Write(option.Value);
                }
            });

        #region Broadcast Channel Packet

        public void BroadcastAsync(ChatMessageResponse value) =>
            Channel.BroadcastAsync(ClientOpcode.ChatMessage, (PacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.WriteChatType(value.Chat);
                writer.WriteByteLengthUnicodeString(value.Message);
            });

        #endregion Broadcast Channel Packet

        #region Broadcast Character Packet

        public void SendAsync(CharacterOthersResponse value) =>
            Session.SendAsync(ClientOpcode.CharacterOtherInfos, (PacketWriter writer) =>
            {
                writer.Write((short)value.Values.Count());
                foreach (CharacterOthersResponse.Entity entity in value.Values)
                {
                    writer.WriteCharacter(entity.Character);
                    writer.WritePlace(entity.Place);
                }
            });

        public void BroadcastAsync(CharacterLevelSet value) =>
            Channel.BroadcastAsync(ClientOpcode.CharacterLevelSet, (PacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write(value.Level);
            });

        public void BroadcastAsync(CharacterToggleWeaponRequest request) =>
            Channel.BroadcastAsync(ClientOpcode.ChatMessage, (PacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Toggle);
                writer.Write(request.Unknown1);
            });

        #endregion Broadcast Character Packet

        #region Broadcast Gesture Packet

        public void BroadcastAsync(CharacterGestureDo value) =>
            Channel.BroadcastAsync(ClientOpcode.GestureDo, (PacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write(value.Gesture);
                writer.WriteVector3(value.Position);
                writer.Write(uint.MinValue);
                writer.Write(value.Rotation);
            });

        #endregion Broadcast Gesture Packet

        #region Broadcast Storage Packet

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

        #endregion Broadcast Storage Packet

        #region Broadcast Movement Packet

        public void BroadcastAsync(MovementMoveRequest request) =>
            Channel.BroadcastAsync(ClientOpcode.MovementMove, (PacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.WriteVector2(request.InterpolatedPosition);
                writer.Write(request.Unknown5);
                writer.Write(request.Unknown6);
                writer.Write(request.Unknown7);
                writer.Write(request.Unknown8);
            });

        public void BroadcastAsync(MovementStopRequest request) =>
            Channel.BroadcastAsync(ClientOpcode.MovementStop, (PacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Unknown4);
                writer.Write(request.Unknown5);
            });

        public void BroadcastAsync(MovementJumpRequest request) =>
            Channel.BroadcastAsync(ClientOpcode.MovementJump, (PacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.Write(request.Unknown2);
                writer.Write(request.Location);
                writer.Write(request.Unknown3);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.WriteVector2(request.InterpolatedPosition);
                writer.Write(request.Unknown5);
                writer.Write(request.Unknown6);
            });

        public void BroadcastAsync(CharacterLoopMotionEndResponse value) =>
            Channel.BroadcastAsync(ClientOpcode.MovementLoopMotionEnd, (PacketWriter writer) =>
                writer.Write(value.Character));

        #endregion Broadcast Movement Packet

        protected BaseChannelMember(TSession session, TChannel channel) =>
            (Session, Channel) = (session, channel);
    }
}