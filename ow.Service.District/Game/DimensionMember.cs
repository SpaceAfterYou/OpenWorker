using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests;
using ow.Service.District.Game.Helpers;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network;
using System;
using System.Linq;

namespace ow.Service.District.Game
{
    public abstract class BaseDimensionMember
    {
    }

    internal sealed class DimensionMember
    {
        private readonly DimensionRepository.Entity _dimension;
        private readonly Session _session;

        internal void Leave() => _dimension.Leave(_session);

        #region Broadcast Channel

        internal void BroadcastAsync(CharacterSpecialOptionListUpdateRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.CharacterSpecialOptionUpdateList, (PacketWriter writer) =>
            {
                foreach ((Guid _, GameSession member) in _dimension.Sessions)
                {
                    if (member is Session session)
                    {
                        if (session.Character.Id != request.Character)
                            continue;

                        writer.Write(session.Character.Id);
                        writer.Write((byte)session.SpecialOptions.Count);

                        foreach (SpecialOptions.Entity option in session.SpecialOptions)
                        {
                            writer.WriteSpecialOption(option.Id);
                            writer.Write(option.Value);
                        }

                        return;
                    }
                }

                // if character not found, send empty

                writer.Write(-1);        // character id
                writer.Write(byte.MinValue);   // count
            });

        internal void BroadcastAsync(ChatReceiveRequest request) =>
            BroadcastAsync(request.Type, request.Message);

        internal void BroadcastAsync(ChatType type, string message) =>
            _dimension.BroadcastAsync(ClientOpcode.ChatMessage, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.WriteChatType(type);
                writer.WriteByteLengthUnicodeString(message);
            });

        #endregion Broadcast Channel

        #region Broadcast Character

        internal void SendCharacterOtherInfos(Instance instance) =>
            _session.SendAsync(ClientOpcode.CharacterOtherInfos, (PacketWriter writer) =>
            {
                /// (.Values) will make copy all sessions in channel
                Session[] sessions = _dimension.Sessions.Values.Cast<Session>().ToArray();

                writer.Write((short)sessions.Length);
                foreach (Session member in sessions.Where(s => s.Id != _session.Id))
                {
                    writer.WriteCharacter(ResponseHelper.GetCharacter(member));
                    writer.WritePlace(ResponseHelper.GetPlace(member, instance));
                }
            });

        internal void BroadcastAsync() =>
            _dimension.BroadcastAsync(ClientOpcode.CharacterLevelSet, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.Write(_session.Character.Level);
            });

        internal void BroadcastAsync(CharacterToggleWeaponRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.ChatMessage, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Toggle);
                writer.Write(request.Unknown1);
            });

        #endregion Broadcast Character

        #region Broadcast Gesture

        internal void BroadcastAsync(GestureDoRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.GestureDo, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.Write(request.Gesture);
                writer.WriteVector3(request.Position);
                writer.Write(request.Unknown1);
                writer.Write(request.Rotation);
            });

        #endregion Broadcast Gesture

        #region Broadcast Storage

        //internal void BroadcastItemUpgradeResponse(BaseItem item)
        //{ }

        //internal void BroadcastItemMove(params BaseItem[] slots)
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

        internal void BroadcastAsync(MovementMoveRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.MovementMove, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.Write(request.Unknown1);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.WriteVector2(request.InterpolatedPosition);
                writer.Write(request.Unknown5);
                writer.Write(request.Unknown6);
                writer.Write(request.Unknown7);
                writer.Write(request.Unknown8);
            });

        internal void BroadcastAsync(MovementStopRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.MovementStop, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
                writer.Write(request.Unknown1);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Unknown4);
                writer.Write(request.Unknown5);
            });

        internal void BroadcastAsync(MovementJumpRequest request) =>
            _dimension.BroadcastAsync(ClientOpcode.MovementJump, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
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

        internal void BroadcastLoopMotionEnd() =>
            _dimension.BroadcastAsync(ClientOpcode.MovementLoopMotionEnd, (PacketWriter writer) =>
            {
                writer.Write(_session.Character.Id);
            });

        #endregion Broadcast Movement

        internal DimensionMember(Session session, DimensionRepository.Entity dimension) => (_session, _dimension) = (session, dimension);
    }
}

// https://youtu.be/7mosZiponDg