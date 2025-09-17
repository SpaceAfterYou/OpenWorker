using System.Xml.Linq;
using System.Xml.XPath;
using OpenWorker.Batch.Entities;

namespace OpenWorker.Batch;

public readonly struct EventBox(XElement x)
{
    public IReadOnlyList<SectorBox> Sectors { get; } = x
        .XPathSelectElements("VSectorBox")
        .Select(v => new SectorBox(v))
        .ToArray();

    public IReadOnlyList<MonsterSpawnBox> MonsterSpawns { get; } = x
        .XPathSelectElements("VMonsterSpawnBox")
        .Select(v => new MonsterSpawnBox(v))
        .ToArray();

    public IReadOnlyList<StartEventBox> StartEvents { get; } = x
        .XPathSelectElements("VStartEventBox")
        .Select(v => new StartEventBox(v))
        .ToArray();

    public IReadOnlyList<PortalExitBox> PortalExits { get; } = x
        .XPathSelectElements("VPortalExitBox")
        .Select(v => new PortalExitBox(v))
        .ToArray();

    public IReadOnlyList<PortalBox> Portals { get; } = x
        .XPathSelectElements("VPortalBox")
        .Select(v => new PortalBox(v))
        .ToArray();

    public IReadOnlyList<MazeEscapeBox> MazeEscapes { get; } = x
        .XPathSelectElements("VMazeEscapeBox")
        .Select(v => new MazeEscapeBox(v))
        .ToArray();

    public IReadOnlyList<SectorStartBox> SectorStarts { get; } = x
        .XPathSelectElements("VSectorStartBox")
        .Select(v => new SectorStartBox(v))
        .ToArray();

    public IReadOnlyList<SocialItemExcludeBox> SocialItemExcludes { get; } = x
        .XPathSelectElements("VSocialItemExcludeBox")
        .Select(v => new SocialItemExcludeBox(v))
        .ToArray();

    public IReadOnlyList<LuaFunctionBox> LuaFunctions { get; } = x
        .XPathSelectElements("VLuaFunctionBox")
        .Select(v => new LuaFunctionBox(v))
        .ToArray();

    public IReadOnlyList<SafeAreaBox> SafeAreas { get; } = x
        .XPathSelectElements("VSafeAreaBox")
        .Select(v => new SafeAreaBox(v))
        .ToArray();

    public IReadOnlyList<PersonalShopAreaBox> PersonalShopAreas { get; } = x
        .XPathSelectElements("VPersonalShopAreaBox")
        .Select(v => new PersonalShopAreaBox(v))
        .ToArray();

    public IReadOnlyList<InterActionBox> InterActions { get; } = x
        .XPathSelectElements("VInterActionBox")
        .Select(v => new InterActionBox(v))
        .ToArray();

    public IReadOnlyList<CheckMonsterSpawnBox> CheckMonsterSpawns { get; } = x
        .XPathSelectElements("VCheckMonsterSpawnBox")
        .Select(v => new CheckMonsterSpawnBox(v))
        .ToArray();

    public IReadOnlyList<CommonPositionBox> CommonPositions { get; } = x
        .XPathSelectElements("VCommonPositionBox")
        .Select(v => new CommonPositionBox(v))
        .ToArray();

    public IReadOnlyList<CheckSectorBox> CheckSectors { get; } = x
        .XPathSelectElements("VCheckSectorBox")
        .Select(v => new CheckSectorBox(v))
        .ToArray();

    public IReadOnlyList<ServerGateBox> ServerGates { get; } = x
        .XPathSelectElements("VServerGateBox")
        .Select(v => new ServerGateBox(v))
        .ToArray();

    public IReadOnlyList<QuestMoveCheckBox> QuestMoveChecks { get; } = x
        .XPathSelectElements("VQuestMoveCheckBox")
        .Select(v => new QuestMoveCheckBox(v))
        .ToArray();

    public IReadOnlyList<CheckEventSpawnBox> CheckEventSpawns { get; } = x
        .XPathSelectElements("VCheckEventSpawnBox")
        .Select(v => new CheckEventSpawnBox(v))
        .ToArray();
}