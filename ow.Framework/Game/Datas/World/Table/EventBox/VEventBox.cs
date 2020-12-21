using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VEventBox
    {
        public IReadOnlyList<VSectorBox> Sectors { get; }
        public IReadOnlyList<VMonsterSpawnBox> MonsterSpawns { get; }
        public IReadOnlyList<VStartEventBox> StartEvents { get; }
        public IReadOnlyList<VPortalExitBox> PortalExits { get; }
        public IReadOnlyList<VPortalBox> Portals { get; }
        public IReadOnlyList<VMazeEscapeBox> MazeEscapes { get; }
        public IReadOnlyList<VSectorStartBox> SectorStarts { get; }
        public IReadOnlyList<VSocialItemExcludeBox> SocialItemExcludes { get; }
        public IReadOnlyList<VLuaFunctionBox> LuaFunctions { get; }
        public IReadOnlyList<VSafeAreaBox> SafeAreas { get; }
        public IReadOnlyList<VPersonalShopAreaBox> PersonalShopAreas { get; }
        public IReadOnlyList<VInterActionBox> InterActions { get; }
        public IReadOnlyList<VCheckMonsterSpawnBox> CheckMonsterSpawns { get; }
        public IReadOnlyList<VCommonPositionBox> CommonPositions { get; }
        public IReadOnlyList<VCheckSectorBox> CheckSectors { get; set; }
        public IReadOnlyList<VServerGateBox> ServerGates { get; }
        public IReadOnlyList<VQuestMoveCheckBox> QuestMoveChecks { get; }
        public IReadOnlyList<VCheckEventSpawnBox> CheckEventSpawns { get; }

        internal VEventBox(XmlNode xml)
        {
            Sectors = xml.SelectNodes("VSectorBox")?.Cast<XmlNode>().Select(v => new VSectorBox(v)).ToArray() ?? Array.Empty<VSectorBox>();
            MonsterSpawns = xml.SelectNodes("VMonsterSpawnBox")?.Cast<XmlNode>().Select(v => new VMonsterSpawnBox(v)).ToArray() ?? Array.Empty<VMonsterSpawnBox>();
            StartEvents = xml.SelectNodes("VStartEventBox")?.Cast<XmlNode>().Select(v => new VStartEventBox(v)).ToArray() ?? Array.Empty<VStartEventBox>();
            PortalExits = xml.SelectNodes("VPortalExitBox")?.Cast<XmlNode>().Select(v => new VPortalExitBox(v)).ToArray() ?? Array.Empty<VPortalExitBox>();
            Portals = xml.SelectNodes("VPortalBox")?.Cast<XmlNode>().Select(v => new VPortalBox(v)).ToArray() ?? Array.Empty<VPortalBox>();
            MazeEscapes = xml.SelectNodes("VMazeEscapeBox")?.Cast<XmlNode>().Select(v => new VMazeEscapeBox(v)).ToArray() ?? Array.Empty<VMazeEscapeBox>();
            SectorStarts = xml.SelectNodes("VSectorStartBox")?.Cast<XmlNode>().Select(v => new VSectorStartBox(v)).ToArray() ?? Array.Empty<VSectorStartBox>();
            SocialItemExcludes = xml.SelectNodes("VSocialItemExcludeBox")?.Cast<XmlNode>().Select(v => new VSocialItemExcludeBox(v)).ToArray() ?? Array.Empty<VSocialItemExcludeBox>();
            LuaFunctions = xml.SelectNodes("VLuaFunctionBox")?.Cast<XmlNode>().Select(v => new VLuaFunctionBox(v)).ToArray() ?? Array.Empty<VLuaFunctionBox>();
            SafeAreas = xml.SelectNodes("VSafeAreaBox")?.Cast<XmlNode>().Select(v => new VSafeAreaBox(v)).ToArray() ?? Array.Empty<VSafeAreaBox>();
            PersonalShopAreas = xml.SelectNodes("VPersonalShopAreaBox")?.Cast<XmlNode>().Select(v => new VPersonalShopAreaBox(v)).ToArray() ?? Array.Empty<VPersonalShopAreaBox>();
            InterActions = xml.SelectNodes("VInterActionBox")?.Cast<XmlNode>().Select(v => new VInterActionBox(v)).ToArray() ?? Array.Empty<VInterActionBox>();
            CheckMonsterSpawns = xml.SelectNodes("VCheckMonsterSpawnBox")?.Cast<XmlNode>().Select(v => new VCheckMonsterSpawnBox(v)).ToArray() ?? Array.Empty<VCheckMonsterSpawnBox>();
            CommonPositions = xml.SelectNodes("VCommonPositionBox")?.Cast<XmlNode>().Select(v => new VCommonPositionBox(v)).ToArray() ?? Array.Empty<VCommonPositionBox>();
            CheckSectors = xml.SelectNodes("VCheckSectorBox")?.Cast<XmlNode>().Select(v => new VCheckSectorBox(v)).ToArray() ?? Array.Empty<VCheckSectorBox>();
            ServerGates = xml.SelectNodes("VServerGateBox")?.Cast<XmlNode>().Select(v => new VServerGateBox(v)).ToArray() ?? Array.Empty<VServerGateBox>();
            QuestMoveChecks = xml.SelectNodes("VQuestMoveCheckBox")?.Cast<XmlNode>().Select(v => new VQuestMoveCheckBox(v)).ToArray() ?? Array.Empty<VQuestMoveCheckBox>();
            CheckEventSpawns = xml.SelectNodes("VCheckEventSpawnBox")?.Cast<XmlNode>().Select(v => new VCheckEventSpawnBox(v)).ToArray() ?? Array.Empty<VCheckEventSpawnBox>();
        }
    }
}
