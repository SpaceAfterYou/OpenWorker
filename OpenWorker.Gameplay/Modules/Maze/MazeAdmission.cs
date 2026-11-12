using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The portals of the map a player stands in, which the client names when it takes one: the server decides whether the
/// player is at it. A server without maps of districts has none (<see cref="MazeAdmission"/> then asks nothing of the place).
/// </summary>
public interface IMazePortals
{
    /// <summary>
    /// Whether the map the player stands in has the portal, and the player stands within reach of it.
    /// </summary>
    bool IsAt(Entity player, int portal);

    /// <summary>
    /// Whether the map the player stands in has the portal.
    /// </summary>
    bool Exists(Entity player, int portal);

    /// <summary>
    /// Where the portal of the map the player stands in leads, as the map says; null when the map has no such portal.
    /// </summary>
    PortalWay? WayOf(Entity player, int portal);
}

/// <summary>
/// Where a portal of a map leads, as the file of the map says and as the client reads it when the player steps in.
/// </summary>
/// <param name="Kind">How the client moves the player: into the location it names (with its start point or the first of
/// the location), onto the maze board, to another place of the map, or into the tower's own dialog.</param>
/// <param name="Location">The location the portal names, 0 for none (a board names its mazes instead).</param>
/// <param name="Start">The start point of the location the portal names, 0 for none.</param>
/// <param name="Board">The maze board the portal opens (the mazes whose row names it are offered there), 0 for none.</param>
public readonly record struct PortalWay(JumpType Kind, short Location, int Start, int Board);

/// <summary>
/// What a maze asks of whoever takes its portal, checked in the order of the reference. A refusal is the error of the request
/// with the code the client knows.
/// <list type="bullet">
/// <item>Whoever takes the portal, alone or for a party (<see cref="CheckEntrant"/>): the soul weapon in hand, a row of the
/// table for the maze, the hours it is open, who may come (alone, a party), the level, the maze cleared before it.</item>
/// <item>A player alone (<see cref="Check"/>) goes on with the portal it stands at, the quest, the daily limit (and for a
/// casual raid the day of the week), the fatigue and the item the maze asks for.</item>
/// <item>Each member of a party (<see cref="CheckParty"/>), when the master takes the portal and before anybody is asked:
/// the level, the weapon, the maze cleared before it, the portal in its map, the daily limit, the item and the fatigue.</item>
/// </list>
/// Not checked: the places the reference refuses to a player in a trade, in the matching or at a card game, a portal that asks
/// for a quest of its own, the net cafe.
/// </summary>
public sealed class MazeAdmission(
    World world,
    ReadOnlyCollection<MazeInfoRow> mazes,
    FatigueService fatigue,
    MazeScoring scoring,
    SharePointManager progress,
    MazeEntryLimits? limits = null,
    InventoryService? inventory = null,
    QuestManager? quests = null,
    IMazePortals? portals = null)
{
    /// <summary>The maze is for a player alone.</summary>
    public const ushort AloneOnly = 55032;

    /// <summary>The maze is for a party.</summary>
    public const ushort PartyOnly = 55033;

    /// <summary>Nobody enters the maze this way.</summary>
    public const ushort Closed = 55052;

    /// <summary>The level of the player is below the level of the maze.</summary>
    public const ushort LevelTooLow = 55012;

    /// <summary>The level of a member of the party is below the level of the maze.</summary>
    public const ushort MemberLevelTooLow = 55013;

    /// <summary>The maze before it is not cleared, or the player stands where no portal is.</summary>
    public const ushort NotOpened = 55003;

    /// <summary>A member of the party has not cleared the maze before it.</summary>
    public const ushort MemberNotOpened = 55048;

    /// <summary>The player has no soul weapon in hand.</summary>
    public const ushort NoWeapon = 55016;

    /// <summary>A member of the party has no soul weapon in hand.</summary>
    public const ushort MemberNoWeapon = 55017;

    /// <summary>The table has no row for the maze.</summary>
    public const ushort NoMaze = 55008;

    /// <summary>The player has neither taken nor finished the quest the maze asks for.</summary>
    public const ushort NoQuest = 55037;

    /// <summary>The player does not carry the item the maze asks for (the answer names the item).</summary>
    public const ushort NoItem = 55035;

    /// <summary>A member of the party does not carry the item the maze asks for (the answer names the item).</summary>
    public const ushort MemberNoItem = 55056;

    /// <summary>A member of the party stands in a map that has no such portal.</summary>
    public const ushort MemberNoPortal = 55055;

    /// <summary>The maze cannot be entered by a party, or has no row to say so.</summary>
    public const ushort CannotEnter = 55001;

    private const byte Anybody = 0;
    private const byte Alone = 1;
    private const byte Party = 2;
    private const byte FullParty = 3;

    /// <summary>The kinds of maze a party cannot enter: the raid of the thing and its like.</summary>
    private const byte NoPartyKind = 5;
    private const byte NoPartyRaid = 11;

    /// <summary>The kind of the tutorial mazes.</summary>
    private const byte TutorialKind = 1;

    /// <summary>The room of a player.</summary>
    private const byte RoomKind = 6;

    /// <summary>A stage of the infinite tower.</summary>
    private const byte TowerKind = 7;

    /// <summary>The map of the raid of the event, which is entered from wherever the event puts the player.</summary>
    private const short EventRaid = 30031;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    /// <summary>
    /// Whether the location is one of the mazes (the ids of the mazes begin with 2); a maze the table has no row for is
    /// refused, a district is not this check's to refuse.
    /// </summary>
    private static bool IsMazeLocation(short location) => location is >= 20000 and < 30000;

    /// <summary>
    /// A player alone takes the portal of a maze or another location. Null when it may enter, else the code of the refusal;
    /// <paramref name="item"/> is what the answer names after the code (the item a maze asks for), 0 for nothing.
    /// </summary>
    public ushort? Check(Entity player, short location, int portal, out int item)
    {
        item = 0;

        if (CheckEntrant(player, location, inParty: false) is { } refused)
        {
            return refused;
        }

        if (!_mazes.TryGet(location, out var row))
        {
            // A district or a map of an event is entered through a portal of the map the player stands in, too.
            return portals is not null && !IsMazeLocation(location) && location != EventRaid && !portals.IsAt(player, portal) ? NotOpened : null;
        }

        // The portal the player takes is in the map it stands in, and it stands at it. The tutorial mazes are entered from
        // wherever the story puts the player.
        if (row.Type != TutorialKind && portals is not null && !portals.IsAt(player, portal))
        {
            return NotOpened;
        }

        return CheckRules(player, row, out item);
    }

    /// <summary>
    /// The way a player takes into a location (04/01) leads where the portal it names leads, as the map says: a client cannot
    /// stand at one portal and name another place or another start point. Null when it does, with <paramref name="start"/>
    /// the start point to enter by (0 for the first of the location); else the code of the refusal.
    /// <list type="bullet">
    /// <item>A portal into a district or out of a maze names the location and its start point, and the client sends both.</item>
    /// <item>The portal into the map of an event names the location and the place each district comes out at; the client
    /// sends no start point and the player comes out at the portal's.</item>
    /// <item>A portal into a maze names the location, entered at its first start point.</item>
    /// <item>A portal that opens the maze board leads into the mazes the board offers (<see cref="CheckBoard"/>), at their
    /// first start point. The client asks for those by 04/41; asking by 04/01 leads nowhere else.</item>
    /// <item>Any other portal (a move within the map, the dialog of the tower) does not lead out of the map.</item>
    /// </list>
    /// The tutorial mazes and the raid of the event are entered from wherever the story or the event puts the player, at the
    /// first start point. A server without maps of districts judges nothing and keeps the start point asked for.
    /// </summary>
    public ushort? CheckWay(Entity player, short location, int portal, int jump, out int start)
    {
        start = 0;

        if (portals is null)
        {
            start = jump;

            return null;
        }

        if (location == EventRaid || (_mazes.TryGet(location, out var row) && row.Type == TutorialKind))
        {
            return null;
        }

        if (portals.WayOf(player, portal) is not { } way)
        {
            return NotOpened;
        }

        var named = way.Location != 0 && location == way.Location;

        switch (way.Kind)
        {
            case JumpType.Field when named && jump == way.Start:
            case JumpType.District6 when named && jump == 0:
                start = way.Start;

                return null;

            case JumpType.Maze when way.Board == 0 && named && jump == 0:
            case JumpType.Maze when way.Board != 0 && jump == 0 && Offers(way.Board, location):
                return null;

            default:
                return NotOpened;
        }
    }

    /// <summary>
    /// The maze board (04/41) leads into a maze it offers: the player names a portal of its map that opens a board, and the
    /// maze is one of the board's. Null when it does, else the code of the refusal. A member of a party that plays a maze is
    /// offered that maze from anywhere; the caller asks the party before this.
    /// </summary>
    public ushort? CheckBoard(Entity player, short location, int portal)
    {
        if (portals is null)
        {
            return null;
        }

        return portals.WayOf(player, portal) is { Kind: JumpType.Maze, Board: not 0 } way && Offers(way.Board, location) ? null : NotOpened;
    }

    /// <summary>
    /// Whether the board offers the maze: the server's row of every maze names the board it is entered by, which holds the
    /// list the client shows (the client's own table of boards) and the casual raids each day of the portal brings.
    /// </summary>
    private bool Offers(int board, short location)
    {
        return IsMazeLocation(location) && _mazes.TryGet(location, out _) && scoring.TryGetRule(location, out var rule) && rule.PortalId == board;
    }

    /// <summary>
    /// What a maze asks of the player besides where they stand: the quest, the daily limit, the fatigue and the item.
    /// </summary>
    private ushort? CheckRules(Entity player, in MazeInfoRow row, out int item)
    {
        item = 0;

        scoring.TryGetRule(row.Id, out var rule);

        // The quest, taken or finished.
        if (rule.RequiredQuest is > 0 and <= int.MaxValue && quests is not null &&
            !quests.HasActiveEpisode(player, (int)rule.RequiredQuest) && !quests.HasFinishedEpisode(player, (int)rule.RequiredQuest))
        {
            return NoQuest;
        }

        if (limits?.Check(player, row.Id) is { } limit)
        {
            return limit;
        }

        if (!fatigue.CanEnter(player, row.Id))
        {
            return FatigueService.NotEnough;
        }

        return LacksItem(player, rule, out item) ? NoItem : null;
    }

    public ushort? Check(Entity player, short location, int portal = 0) => Check(player, location, portal, out _);

    /// <summary>
    /// A player waits for a party into a maze. The wait asks of the player what the portal of the maze asks of a member of
    /// a party, wherever the player stands: the weapon, the hours, the level, the maze before it, the quest, the daily limit,
    /// the fatigue and the item. Null when the player may wait, else the code of the refusal.
    /// </summary>
    public ushort? CheckMatching(Entity player, short location, out int item)
    {
        item = 0;

        if (CheckEntrant(player, location, inParty: true) is { } refused)
        {
            return refused;
        }

        return _mazes.TryGet(location, out var row) ? CheckRules(player, row, out item) : null;
    }

    /// <summary>
    /// What stands before everything else at the portal of a maze, for whoever takes it: the weapon, a row of the table, the
    /// hours, who may come, the level and the maze cleared before it. A location that is not a maze asks for nothing here.
    /// </summary>
    public ushort? CheckEntrant(Entity player, short location, bool inParty)
    {
        if (!IsMazeLocation(location))
        {
            // The events of the 3xxxx maps are entered with a weapon, as mazes are.
            return location >= 30000 && NoSoulWeapon(player) ? NoWeapon : null;
        }

        if (NoSoulWeapon(player))
        {
            return NoWeapon;
        }

        if (!_mazes.TryGet(location, out var row))
        {
            return NoMaze;
        }

        // A stage of the tower and the room of a player are entered by their own requests, which count the day, keep the
        // order of the stages and find the room: a portal or a maze board never leads into one.
        if (row.Type is TowerKind or RoomKind)
        {
            return Closed;
        }

        if (limits is not null && !limits.IsOpen(row.Id))
        {
            return MazeEntryLimits.Closed;
        }

        if (inParty)
        {
            if (row.Admission == Alone)
            {
                return AloneOnly;
            }

            if (row.Admission is not (Anybody or Party or FullParty))
            {
                return Closed;
            }
        }

        else
        {
            if (row.Admission is Party or FullParty)
            {
                return PartyOnly;
            }

            if (row.Admission is not (Anybody or Alone))
            {
                return Closed;
            }
        }

        var level = LevelOf(player);

        if (row.RequiredLevel > 0 && level < row.RequiredLevel)
        {
            return LevelTooLow;
        }

        return NotCleared(player, row) ? NotOpened : null;
    }

    /// <summary>
    /// The master of a party takes the portal of a maze for the party. Whoever takes the portal passes what is asked of
    /// everybody (<see cref="CheckEntrant"/>), the maze must take a party, and every member must be able to enter; null when
    /// they may, else the code of the refusal for the master.
    /// </summary>
    public ushort? CheckParty(Entity entrant, IReadOnlyList<Entity> members, short location, int portal, out int item)
    {
        item = 0;

        if (CheckEntrant(entrant, location, inParty: true) is { } refused)
        {
            return refused;
        }

        if (!_mazes.TryGet(location, out var row))
        {
            return null;
        }

        // The raid of the thing and its like are not for a party.
        if (row.Type is NoPartyKind or NoPartyRaid)
        {
            return CannotEnter;
        }

        scoring.TryGetRule(row.Id, out var rule);

        foreach (var member in members)
        {
            if (CheckMember(member, row, rule, portal, out item) is { } code)
            {
                return code;
            }
        }

        return null;
    }

    public ushort? CheckParty(Entity entrant, IReadOnlyList<Entity> members, short location, int portal = 0) =>
        CheckParty(entrant, members, location, portal, out _);

    private ushort? CheckMember(Entity member, in MazeInfoRow row, in MazeRuleRow rule, int portal, out int item)
    {
        item = 0;

        if (!world.IsAlive(member))
        {
            return null;
        }

        if (row.RequiredLevel > 0 && LevelOf(member) < row.RequiredLevel)
        {
            return MemberLevelTooLow;
        }

        if (NoSoulWeapon(member))
        {
            return MemberNoWeapon;
        }

        if (NotCleared(member, row))
        {
            return MemberNotOpened;
        }

        if (portals is not null && !portals.Exists(member, portal))
        {
            return MemberNoPortal;
        }

        if (limits?.Check(member, row.Id) is { } limit)
        {
            return limit;
        }

        if (LacksItem(member, rule, out item))
        {
            return MemberNoItem;
        }

        return fatigue.CanEnter(member, row.Id) ? null : FatigueService.NotEnough;
    }

    private int LevelOf(Entity player)
    {
        return world.IsAlive(player) && world.TryGet<LevelComponent>(player, out var rank) ? rank.Level : 0;
    }

    /// <summary>
    /// Whether the player has no soul weapon in hand. A player the server has no inventory for is not judged.
    /// </summary>
    private bool NoSoulWeapon(Entity player)
    {
        return inventory?.InventoryOf(player) is { } bag && bag.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon) is null;
    }

    /// <summary>
    /// Whether the maze before this one is named in the server's row and the player has not cleared it.
    /// </summary>
    private bool NotCleared(Entity player, in MazeInfoRow row)
    {
        // The maze before it is named only in the server's own row of the maze.
        return scoring.TryGetRule(row.Id, out var rule) && rule.RequiredMaze is > 0 and <= int.MaxValue &&
               world.IsAlive(player) && !progress.HasCleared(player, (int)rule.RequiredMaze);
    }

    /// <summary>
    /// Whether the maze asks for an item the player does not carry in the number asked.
    /// </summary>
    private bool LacksItem(Entity player, in MazeRuleRow rule, out int item)
    {
        item = 0;

        if (rule.RequiredItem is 0 or > int.MaxValue || inventory is null)
        {
            return false;
        }

        item = (int)rule.RequiredItem;

        return inventory.CountOf(player, item) < Math.Max(1, (int)rule.RequiredItemCount);
    }
}
