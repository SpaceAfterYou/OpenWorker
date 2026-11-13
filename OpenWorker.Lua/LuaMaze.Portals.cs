using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Movement;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// The portals that lead to another place on the map. Everyone in the maze goes through them together, so a portal waits
/// for the others (<see cref="PortalWait"/>) and takes them all along when it opens.
/// </summary>
public partial class LuaMaze
{
    private PortalWait? _portalWait;

    private PortalWait Portals => _portalWait ??= new PortalWait(() => _members.Count, TellPortalCountdown, WarpThroughPortal);

    /// <summary>
    /// Whether a countdown to the opening of a portal runs.
    /// </summary>
    [LuaIgnoreMember]
    public bool IsPortalCounting => _portalWait is { IsCounting: true };

    /// <summary>
    /// How many players stand at the portal and wait for the others.
    /// </summary>
    [LuaIgnoreMember]
    public int WaitingAtPortal(int portal) => _portalWait?.WaitingAt(portal) ?? 0;

    /// <summary>
    /// The sector the position lies in, if any.
    /// </summary>
    [LuaIgnoreMember]
    public SectorBox? SectorAt(Vector3 position)
    {
        return Batch.EventBox.Sectors.FirstOrDefault(e =>
            position.X >= Math.Min(e.PosTopLeft.X, e.PosBottomRight.X) &&
            position.X <= Math.Max(e.PosTopLeft.X, e.PosBottomRight.X) &&
            position.Y >= Math.Min(e.PosTopLeft.Y, e.PosBottomRight.Y) &&
            position.Y <= Math.Max(e.PosTopLeft.Y, e.PosBottomRight.Y));
    }

    /// <summary>
    /// The place a portal leads to, by the number the client names it with.
    /// </summary>
    private BasicEntity? ExitOf(int jump)
    {
        var events = Batch.EventBox;

        BasicEntity? destination = events.PortalExits.Find(jump);
        destination ??= events.SectorStarts.Find(jump);
        destination ??= events.StartEvents.Find(jump);

        return destination;
    }

    /// <summary>
    /// A player steps into a portal that leads to another place on the map. False when the portal cannot be used from where
    /// the player stands: it is shut (the portal of a boss sector never is), on another map, or leads nowhere. The portal
    /// opens at once when everyone in the maze stands at it; otherwise the others are waited for.
    /// </summary>
    [LuaIgnoreMember]
    public bool EnterPortal(Entity player, int location, int portal, int jump)
    {
        if (Closed || Find(player) is null || !Ecs.IsAlive(player))
        {
            return false;
        }

        var world = Ecs.Get<WorldComponent>(player);

        // The portal of a boss sector is never shut: whoever fights the boss must be able to leave.
        var inBossSector = SectorAt(world.Position)?.Type == SectorType.Boss;

        // Where a portal leads is what the map says of it, not what the client names: a client that could name the exit
        // would go through one open portal to any place of the map.
        if (Batch.EventBox.Portals.Find(portal) is not { JumpType: JumpType.JumpPos } box)
        {
            return false;
        }

        if (location != world.Location || !(inBossSector || Progress.IsPortalOpen(portal)) || ExitOf(box.Jump) is null)
        {
            return false;
        }

        Portals.Add(player, new PortalWait.Door(portal, box.Jump));

        return true;
    }

    /// <summary>
    /// A player steps out of the portal they stood at, and is not waited for any more.
    /// </summary>
    [LuaIgnoreMember]
    public void LeavePortal(Entity player)
    {
        _portalWait?.Remove(player);
    }

    private void TellPortalCountdown(int seconds)
    {
        creatureManager.Broadcast(new WorldWarpMazeTimeResponse { Time = TimeSpan.FromSeconds(seconds) });
    }

    /// <summary>
    /// The portal opens: everyone in the maze comes out of it at the same place, which the others are told.
    /// </summary>
    private void WarpThroughPortal(PortalWait.Door door)
    {
        if (Closed || ExitOf(door.Jump) is not { } destination)
        {
            return;
        }

        var position = destination.GetRandomPosition();
        var portal = Batch.EventBox.Portals.Find(door.Portal);

        foreach (var member in _members)
        {
            if (Ecs.IsAlive(member.Player) && Ecs.Has<WorldComponent>(member.Player))
            {
                ServerPlace.Put(Ecs, member.Player, position, destination.Rotation);
            }
        }

        creatureManager.Broadcast(new WorldWarpMazeResponse
        {
            BoxIndex = door.Portal,
            Warp = new WarpValue { HasError = false, Position = position, Rotation = destination.Rotation },
            // The map says which sector the portal leads to, and sectors may lie inside one another.
            Sector = portal is { NextSector: > 0 } ? portal.NextSector : SectorAt(position)?.Id ?? 0
        });

        SetPotalFlag(door.Portal, false);

        // A portal that names no sector woke nothing when it opened: the sector the players come to wakes now, as the first
        // sector does when the maze has loaded. A sector already awake spawns nothing twice.
        if (portal is not { NextSector: > 0 } && SectorAt(position) is { } arrived)
        {
            SpawnSector(arrived.Id);
        }
    }

    [LuaMember]
    public void AllUserWarpInSector(VVector3 position, float rotation)
    {
        var place = new Vector3(position.X, position.Y, position.Z);

        foreach (var member in _members)
        {
            if (Ecs.IsAlive(member.Player) && Ecs.Has<WorldComponent>(member.Player))
            {
                ServerPlace.Put(Ecs, member.Player, place, rotation);
            }
        }

        creatureManager.Broadcast(new MazeWarpInSectorResponse { Position = place, Rotation = rotation });
    }

    [LuaMember]
    public void SetPotalFlag(int box, bool flag)
    {
        if (Progress.SetPortal(box, flag))
        {
            creatureManager.Broadcast(new WorldWarpEnableResponse { Box = box, Open = flag, IsMazeComplete = IsComplete });

            if (flag && Batch.EventBox.Portals.Find(box) is { NextSector: > 0 } portal)
            {
                SpawnSector(portal.NextSector);
            }
        }
    }
}
