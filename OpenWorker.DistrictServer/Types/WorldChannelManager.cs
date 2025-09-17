using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.DistrictServer.Types;

// public sealed class WorldChannelManager(short id, int index)
// {
//     private const short Full = 64;
//     private const short High = 48;
//     private const short Normal = 32;
//
//     public short Id { get; } = id;
//
//     public ChannelWorkload Workload =>
//         PrivateMembers.Count switch
//         {
//             > Full => ChannelWorkload.Full,
//             > High => ChannelWorkload.High,
//             > Normal => ChannelWorkload.Normal,
//             _ => ChannelWorkload.Low
//         };
//
//     public IEnumerable<Entity> Members => PrivateMembers;
//     public bool CanJoin => PrivateMembers.Count < Full;
//
//     private int Index { get; } = index;
//     private HashSet<Entity> PrivateMembers { get; } = [];
//
//     public void BroadcastMovement(Entity entity)
//     {
//         var me = world.Get<ActorComponent>(entity);
//
//         foreach (var member in PrivateMembers.Where(e => e.Get<ActorComponent>().Id != me.Id))
//         {
//             // TODO: broadcast movement
//         }
//     }
//
//     public void BroadcastJump(Entity entity)
//     {
//         var me = world.Get<ActorComponent>(entity);
//
//         foreach (var member in PrivateMembers.Where(e => e.Get<ActorComponent>().Id != me.Id))
//         {
//             // TODO: broadcast movement
//         }
//     }
//
//     public void BroadcastStop(Entity entity)
//     {
//         var me = world.Get<ActorComponent>(entity);
//
//         foreach (var member in PrivateMembers.Where(e => e.Get<ActorComponent>().Id != me.Id))
//         {
//             // TODO: broadcast movement
//         }
//     }
//
//     public void BroadcastMessage(Entity entity, string message)
//     {
//         var me = world.Get<ActorComponent>(entity);
//
//         foreach (var session in PrivateMembers.Select(member => member.Get<ServerSession>()))
//         {
//             session.Send(new ChatNormalResponse(me, ChatMessageAppearance.Normal, message));
//         }
//     }
//
//     public void LeaveAsync(Entity entity)
//     {
//         if (PrivateMembers.Remove(entity) is false)
//         {
//             throw new WorldChannelException();
//         }
//
//         // entity.Remove<WorldChannelComponent>();
//
//         BroadcastLeave(entity);
//     }
//
//     public void Assign(Entity entity)
//     {
//         PrivateMembers.Remove(entity); // TODO: delete this after debug
//
//         if (PrivateMembers.Add(entity) is false)
//         {
//             throw new WorldChannelException();
//         }
//
//         entity.Set(new WorldChannelComponent(Index));
//
//         BroadcastEnter(entity);
//     }
//
//     public void JoinAsync(Entity entity)
//     {
//         var session = world.Get<ServerSessionComponent>(entity);
//
//         Assign(entity);
//
//         // session.Send(new ChannelChangeResponse(new EnterMapResultValue(entity)));
//
//         SendOtherList(entity);
//     }
//
//     public bool TryAssign(Entity entity)
//     {
//         if (CanJoin is false)
//         {
//             return false;
//         }
//
//         Assign(entity);
//
//         return true;
//     }
//
//     public bool TryJoin(Entity entity)
//     {
//         var session = world.Get<ServerSessionComponent>(entity);
//
//         if (TryAssign(entity) is false)
//         {
//             return false;
//         }
//
//         // session.Send(new ChannelChangeResponse(new EnterMapResultValue(entity)));
//
//         SendOtherList(entity);
//
//         return true;
//     }
//
//     private void BroadcastLeave(Entity entity)
//     {
//         foreach (var member in PrivateMembers)
//         {
//             // TODO: broadcast other Player Character leave
//         }
//     }
//
//     private void BroadcastEnter(Entity entity)
//     {
//         foreach (var member in PrivateMembers)
//         {
//             // TODO: send member to me
//         }
//     }
//
//     private void SendOtherList(Entity entity)
//     {
//         foreach (var member in PrivateMembers)
//         {
//             // TODO: send member to me
//         }
//     }
// }