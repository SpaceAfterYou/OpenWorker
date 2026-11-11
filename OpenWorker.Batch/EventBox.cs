using System.Xml.Linq;
using System.Xml.XPath;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch;

/// <summary>
/// The boxes of a map, each kind with an index by id; the spawn boxes and the starts of sectors are also found by sector.
/// Read once with the map and shared by every maze on it.
/// </summary>
public sealed class EventBox(XElement x)
{
    public BoxList<SectorBox> Sectors { get; } = new(x
        .XPathSelectElements("VSectorBox")
        .Select(v => new SectorBox(v)));

    public BoxList<MonsterSpawnBox> MonsterSpawns { get; } = new(x
        .XPathSelectElements("VMonsterSpawnBox")
        .Select(v => new MonsterSpawnBox(v)));

    public BoxList<StartEventBox> StartEvents { get; } = new(x
        .XPathSelectElements("VStartEventBox")
        .Select(v => new StartEventBox(v)));

    public BoxList<PortalExitBox> PortalExits { get; } = new(x
        .XPathSelectElements("VPortalExitBox")
        .Select(v => new PortalExitBox(v)));

    public BoxList<PortalBox> Portals { get; } = new(x
        .XPathSelectElements("VPortalBox")
        .Select(v => new PortalBox(v)));

    public BoxList<MazeEscapeBox> MazeEscapes { get; } = new(x
        .XPathSelectElements("VMazeEscapeBox")
        .Select(v => new MazeEscapeBox(v)));

    public BoxList<SectorStartBox> SectorStarts { get; } = new(x
        .XPathSelectElements("VSectorStartBox")
        .Select(v => new SectorStartBox(v)));

    public BoxList<SocialItemExcludeBox> SocialItemExcludes { get; } = new(x
        .XPathSelectElements("VSocialItemExcludeBox")
        .Select(v => new SocialItemExcludeBox(v)));

    public BoxList<LuaFunctionBox> LuaFunctions { get; } = new(x
        .XPathSelectElements("VLuaFunctionBox")
        .Select(v => new LuaFunctionBox(v)));

    public BoxList<SafeAreaBox> SafeAreas { get; } = new(x
        .XPathSelectElements("VSafeAreaBox")
        .Select(v => new SafeAreaBox(v)));

    public BoxList<PersonalShopAreaBox> PersonalShopAreas { get; } = new(x
        .XPathSelectElements("VPersonalShopAreaBox")
        .Select(v => new PersonalShopAreaBox(v)));

    public BoxList<InterActionBox> InterActions { get; } = new(x
        .XPathSelectElements("VInterActionBox")
        .Select(v => new InterActionBox(v)));

    public BoxList<CheckMonsterSpawnBox> CheckMonsterSpawns { get; } = new(x
        .XPathSelectElements("VCheckMonsterSpawnBox")
        .Select(v => new CheckMonsterSpawnBox(v)));

    public BoxList<CommonPositionBox> CommonPositions { get; } = new(x
        .XPathSelectElements("VCommonPositionBox")
        .Select(v => new CommonPositionBox(v)));

    public BoxList<CheckSectorBox> CheckSectors { get; } = new(x
        .XPathSelectElements("VCheckSectorBox")
        .Select(v => new CheckSectorBox(v)));

    public BoxList<ServerGateBox> ServerGates { get; } = new(x
        .XPathSelectElements("VServerGateBox")
        .Select(v => new ServerGateBox(v)));

    public BoxList<QuestMoveCheckBox> QuestMoveChecks { get; } = new(x
        .XPathSelectElements("VQuestMoveCheckBox")
        .Select(v => new QuestMoveCheckBox(v)));

    public BoxList<CheckEventSpawnBox> CheckEventSpawns { get; } = new(x
        .XPathSelectElements("VCheckEventSpawnBox")
        .Select(v => new CheckEventSpawnBox(v)));

    private ILookup<int, MonsterSpawnBox>? _spawnsOfSector;
    private ILookup<int, SectorStartBox>? _startsOfSector;

    /// <summary>
    /// The spawn boxes of a sector, in the order of the map; none for a sector the map does not have.
    /// </summary>
    public IEnumerable<MonsterSpawnBox> SpawnsOf(int sector)
    {
        return (_spawnsOfSector ??= MonsterSpawns.ToLookup(e => e.Sector))[sector];
    }

    /// <summary>
    /// The first start box of a sector, or nothing.
    /// </summary>
    public SectorStartBox? StartOf(int sector)
    {
        return (_startsOfSector ??= SectorStarts.ToLookup(e => e.Sector))[sector].FirstOrDefault();
    }

    /// <summary>
    /// Every event box of the map, in the order of the kinds below.
    /// </summary>
    public IEnumerable<BasicEntity> AllBoxes
    {
        get
        {
            foreach (var box in Sectors) yield return box;
            foreach (var box in MonsterSpawns) yield return box;
            foreach (var box in StartEvents) yield return box;
            foreach (var box in PortalExits) yield return box;
            foreach (var box in Portals) yield return box;
            foreach (var box in MazeEscapes) yield return box;
            foreach (var box in SectorStarts) yield return box;
            foreach (var box in SocialItemExcludes) yield return box;
            foreach (var box in LuaFunctions) yield return box;
            foreach (var box in SafeAreas) yield return box;
            foreach (var box in PersonalShopAreas) yield return box;
            foreach (var box in InterActions) yield return box;
            foreach (var box in CheckMonsterSpawns) yield return box;
            foreach (var box in CommonPositions) yield return box;
            foreach (var box in CheckSectors) yield return box;
            foreach (var box in ServerGates) yield return box;
            foreach (var box in QuestMoveChecks) yield return box;
            foreach (var box in CheckEventSpawns) yield return box;
        }
    }
}
