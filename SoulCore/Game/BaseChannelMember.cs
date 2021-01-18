using SoulCore.IO.Network.Sync;
using SoulCore.IO.Network.Sync.Commands;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using System.Linq;

namespace SoulCore.Game
{
    public abstract class BaseChannelMember<TChannel, TSession>
        where TChannel : BaseChannel<TSession>
        where TSession : SSessionBase
    {
        protected readonly TChannel Channel;
        protected readonly TSession Session;

        #region Send Character Packet

        public void SendDeferred(CharacterSpecialOptionListUpdateResponse value) =>
            Session.SendDeferred(SCCategory.Character, SCCharacter.UpdateSpecialOptionList, (SPacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write((byte)value.Values.Count());

                foreach (CharacterSpecialOptionListUpdateResponse.Entity option in value.Values)
                {
                    writer.WriteSpecialOption(option.Id);
                    writer.Write(option.Value);
                }
            });

        #endregion Send Character Packet

        #region Broadcast Chat Packet

        public void BroadcastDeferred(ChatMessageResponse value) =>
            Channel.BroadcastDeferred(SCCategory.Chat, SCChat.Normal, (SPacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.WriteChatType(value.Chat);
                writer.WriteByteLengthUnicodeString(value.Message);
            });

        #endregion Broadcast Chat Packet

        #region Broadcast World Packet

        public void SendDeferred(CharacterOthersResponse value) =>
            Session.SendDeferred(SCCategory.World, SCWorld.OtherInfosPc, (SPacketWriter writer) =>
            {
                writer.Write((short)value.Values.Count());
                foreach (CharacterOthersResponse.Entity entity in value.Values)
                {
                    writer.WriteCharacter(entity.Character);
                    writer.WritePlace(entity.Place);
                }
            });

        #endregion Broadcast World Packet

        #region Broadcast Character Packet

        public void BroadcastDeferred(CharacterLevelSet value) =>
            Channel.BroadcastDeferred(SCCategory.Character, SCCharacter.Level, (SPacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write(value.Level);
            });

        #endregion Broadcast Character Packet

        #region Broadcast Gesture Packet

        public void BroadcastDeferred(CharacterGestureDo value) =>
            Channel.BroadcastDeferred(SCCategory.Gesture, SCGesture.Show, (SPacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.Write(value.Gesture);
                writer.Write(value.Position);
                writer.Write(uint.MinValue);
                writer.Write(value.Rotation);
            });

        #endregion Broadcast Gesture Packet

        // #region Broadcast Storage Packet

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

        // #endregion Broadcast Storage Packet

        #region Broadcast Movement Packet

        public void BroadcastDeferred(CharacterToggleWeaponRequest request) =>
            Channel.BroadcastDeferred(SCCategory.Move, SCMove.BattleBt, (SPacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Toggle);
                writer.Write(request.Unknown1);
            });

        public void BroadcastDeferred(SRMMoveRequest request) =>
            Channel.BroadcastDeferred(SCCategory.Move, SCMove.MoveBt, (SPacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.Write(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.InterpolatedPosition);
                writer.Write(request.Unknown5);
                writer.Write(request.Unknown6);
                writer.Write(request.Unknown7);
                writer.Write(request.Unknown8);
            });

        public void BroadcastDeferred(SRMStopRequest request) =>
            Channel.BroadcastDeferred(SCCategory.Move, SCMove.StopBt, (SPacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.Write(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Unknown4);
                writer.Write(request.Unknown5);
            });

        public void BroadcastDeferred(SRMJumpRequest request) =>
            Channel.BroadcastDeferred(SCCategory.Move, SCMove.JumpBt, (SPacketWriter writer) =>
            {
                writer.Write(request.Character);
                writer.Write(request.Unknown1);
                writer.Write(request.Unknown2);
                writer.Write(request.Location);
                writer.Write(request.Unknown3);
                writer.Write(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.InterpolatedPosition);
                writer.Write(request.Unknown5);
                writer.Write(request.Unknown6);
            });

        public void BroadcastDeferred(CharacterLoopMotionEndResponse value) =>
            Channel.BroadcastDeferred(SCCategory.Move, SCMove.LoopMotionEndBt, (SPacketWriter writer) =>
                writer.Write(value.Character));

        #endregion Broadcast Movement Packet

        protected BaseChannelMember(TSession session, TChannel channel) =>
            (Session, Channel) = (session, channel);
    }
}
