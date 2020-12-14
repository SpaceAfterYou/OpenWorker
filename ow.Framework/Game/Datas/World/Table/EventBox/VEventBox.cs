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
            Sectors = xml.SelectNodes("VSectorBox").Cast<XmlNode>().Select(v => new VSectorBox(v)).ToArray();
            MonsterSpawns = xml.SelectNodes("VMonsterSpawnBox").Cast<XmlNode>().Select(v => new VMonsterSpawnBox(v)).ToArray();
            StartEvents = xml.SelectNodes("VStartEventBox").Cast<XmlNode>().Select(v => new VStartEventBox(v)).ToArray();
            PortalExits = xml.SelectNodes("VPortalExitBox").Cast<XmlNode>().Select(v => new VPortalExitBox(v)).ToArray();
            Portals = xml.SelectNodes("VPortalBox").Cast<XmlNode>().Select(v => new VPortalBox(v)).ToArray();
            MazeEscapes = xml.SelectNodes("VMazeEscapeBox").Cast<XmlNode>().Select(v => new VMazeEscapeBox(v)).ToArray();
            SectorStarts = xml.SelectNodes("VSectorStartBox").Cast<XmlNode>().Select(v => new VSectorStartBox(v)).ToArray();
            SocialItemExcludes = xml.SelectNodes("VSocialItemExcludeBox").Cast<XmlNode>().Select(v => new VSocialItemExcludeBox(v)).ToArray();
            LuaFunctions = xml.SelectNodes("VLuaFunctionBox").Cast<XmlNode>().Select(v => new VLuaFunctionBox(v)).ToArray();
            SafeAreas = xml.SelectNodes("VSafeAreaBox").Cast<XmlNode>().Select(v => new VSafeAreaBox(v)).ToArray();
            PersonalShopAreas = xml.SelectNodes("VPersonalShopAreaBox").Cast<XmlNode>().Select(v => new VPersonalShopAreaBox(v)).ToArray();
            InterActions = xml.SelectNodes("VInterActionBox").Cast<XmlNode>().Select(v => new VInterActionBox(v)).ToArray();
            CheckMonsterSpawns = xml.SelectNodes("VCheckMonsterSpawnBox").Cast<XmlNode>().Select(v => new VCheckMonsterSpawnBox(v)).ToArray();
            CommonPositions = xml.SelectNodes("VCommonPositionBox").Cast<XmlNode>().Select(v => new VCommonPositionBox(v)).ToArray();
            CheckSectors = xml.SelectNodes("VCheckSectorBox").Cast<XmlNode>().Select(v => new VCheckSectorBox(v)).ToArray();
            ServerGates = xml.SelectNodes("VServerGateBox").Cast<XmlNode>().Select(v => new VServerGateBox(v)).ToArray();
            QuestMoveChecks = xml.SelectNodes("VQuestMoveCheckBox").Cast<XmlNode>().Select(v => new VQuestMoveCheckBox(v)).ToArray();
            CheckEventSpawns = xml.SelectNodes("VCheckEventSpawnBox").Cast<XmlNode>().Select(v => new VCheckEventSpawnBox(v)).ToArray();
        }
    }
}
