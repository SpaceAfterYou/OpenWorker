using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.Defines;
using SoulWorkerResearch.SoulCore.IO.Net.Messages;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Move;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Gesture;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Chat;
using System.Numerics;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed class InstanceChannel
{
    internal ushort Id { get; init; }
    private HashSet<IHotSpotSession> Sessions { get; } = new();

    private readonly ushort _maxSessionPerChannel;

    internal ChannelLoadStatus Status
    {
        get
        {
            if (Sessions.Count < _maxSessionPerChannel / 1.8f)
            {
                return ChannelLoadStatus.Low;
            }

            if (Sessions.Count < _maxSessionPerChannel / 1.5f)
            {
                return ChannelLoadStatus.Medium;
            }

            if (Sessions.Count < _maxSessionPerChannel / 1.2f)
            {
                return ChannelLoadStatus.High;
            }

            return ChannelLoadStatus.Full;
        }
    }

    public InstanceChannel(ushort maxSessionPerChannel) => _maxSessionPerChannel = maxSessionPerChannel;

    //internal async ValueTask JoinChannel(CancellationToken ct = default) { }

    //internal async ValueTask LeaveChannel(CancellationToken ct = default) { }

    //internal async ValueTask SwitchChannel(CancellationToken ct = default) { }

    internal async ValueTask BroadcastChat(int person, ChatType type, string message, CancellationToken ct = default) => 
        await Broadcast(new ChatNormalClientMessage { Character = person, ChatType = type, Message = message }, ct);

    internal async ValueTask BroadcastMovement(MoveStopBidirectionalMessage message, CancellationToken ct = default) => await Broadcast(message, ct);
    internal async ValueTask BroadcastMovement(MoveJumpBidirectionalMessage message, CancellationToken ct = default) => await Broadcast(message, ct);
    internal async ValueTask BroadcastMovement(MoveMoveBidirectionalMessage message, CancellationToken ct = default) => await Broadcast(message, ct);

    internal async ValueTask BroadcastGesture(int id, int person, Vector3 position, Vector2 rotation, CancellationToken ct = default) =>
        await Broadcast(new GestureShowClientMessage
        {
            Character = person,
            Gesture = id,
            Location = new()
            {
                Position = position,
                Rotation = rotation,
            }
        }, ct);

    private Task Broadcast<T>(T message, CancellationToken ct = default) where T : IBinaryOutcomingMessage => Parallel
        .ForEachAsync(Sessions, ct, async (session, ct) => await session.SendAsync(message, ct));
}
