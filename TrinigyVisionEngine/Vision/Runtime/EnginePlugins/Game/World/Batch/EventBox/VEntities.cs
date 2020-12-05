using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VEntities
    {
        public IReadOnlyList<VSectorBox> Sector { get; }
        public IReadOnlyList<VMonsterSpawnBox> MonsterSpawn { get; }
        public IReadOnlyList<VStartEventBox> StartEvent { get; }
        public IReadOnlyList<VPortalExitBox> PortalExit { get; }
        public IReadOnlyList<VPortalBox> Portal { get; }
        public IReadOnlyList<VMazeEscapeBox> MazeEscape { get; }
        public IReadOnlyList<VSectorStartBox> SectorStart { get; }
        public IReadOnlyList<VSocialItemExcludeBox> SocialItemExclude { get; }
        public IReadOnlyList<VLuaFunctionBox> LuaFunction { get; }
        public IReadOnlyList<VSafeAreaBox> SafeArea { get; }
        public IReadOnlyList<VPersonalShopAreaBox> PersonalShopArea { get; }
        public IReadOnlyList<VInterActionBox> InterAction { get; }
        public IReadOnlyList<VCheckMonsterSpawnBox> CheckMonsterSpawn { get; }
        public IReadOnlyList<VCommonPositionBox> CommonPosition { get; }
        public IReadOnlyList<VCheckSectorBox> CheckSector { get; set; }
        public IReadOnlyList<VServerGateBox> ServerGate { get; }
        public IReadOnlyList<VQuestMoveCheckBox> QuestMoveCheck { get; }
        public IReadOnlyList<VCheckEventSpawnBox> CheckEventSpawn { get; }

        internal VEntities(XmlNode xml)
        {
            Sector = xml.SelectNodes("VSectorBox").Cast<XmlNode>().Select(v => new VSectorBox(v)).ToArray();
            MonsterSpawn = xml.SelectNodes("VMonsterSpawnBox").Cast<XmlNode>().Select(v => new VMonsterSpawnBox(v)).ToArray();
            StartEvent = xml.SelectNodes("VStartEventBox").Cast<XmlNode>().Select(v => new VStartEventBox(v)).ToArray();
            PortalExit = xml.SelectNodes("VPortalExitBox").Cast<XmlNode>().Select(v => new VPortalExitBox(v)).ToArray();
            Portal = xml.SelectNodes("VPortalBox").Cast<XmlNode>().Select(v => new VPortalBox(v)).ToArray();
            MazeEscape = xml.SelectNodes("VMazeEscapeBox").Cast<XmlNode>().Select(v => new VMazeEscapeBox(v)).ToArray();
            SectorStart = xml.SelectNodes("VSectorStartBox").Cast<XmlNode>().Select(v => new VSectorStartBox(v)).ToArray();
            SocialItemExclude = xml.SelectNodes("VSocialItemExcludeBox").Cast<XmlNode>().Select(v => new VSocialItemExcludeBox(v)).ToArray();
            LuaFunction = xml.SelectNodes("VLuaFunctionBox").Cast<XmlNode>().Select(v => new VLuaFunctionBox(v)).ToArray();
            SafeArea = xml.SelectNodes("VSafeAreaBox").Cast<XmlNode>().Select(v => new VSafeAreaBox(v)).ToArray();
            PersonalShopArea = xml.SelectNodes("VPersonalShopAreaBox").Cast<XmlNode>().Select(v => new VPersonalShopAreaBox(v)).ToArray();
            InterAction = xml.SelectNodes("VInterActionBox").Cast<XmlNode>().Select(v => new VInterActionBox(v)).ToArray();
            CheckMonsterSpawn = xml.SelectNodes("VCheckMonsterSpawnBox").Cast<XmlNode>().Select(v => new VCheckMonsterSpawnBox(v)).ToArray();
            CommonPosition = xml.SelectNodes("VCommonPositionBox").Cast<XmlNode>().Select(v => new VCommonPositionBox(v)).ToArray();
            CheckSector = xml.SelectNodes("VCheckSectorBox").Cast<XmlNode>().Select(v => new VCheckSectorBox(v)).ToArray();
            ServerGate = xml.SelectNodes("VServerGateBox").Cast<XmlNode>().Select(v => new VServerGateBox(v)).ToArray();
            QuestMoveCheck = xml.SelectNodes("VQuestMoveCheckBox").Cast<XmlNode>().Select(v => new VQuestMoveCheckBox(v)).ToArray();
            CheckEventSpawn = xml.SelectNodes("VCheckEventSpawnBox").Cast<XmlNode>().Select(v => new VCheckEventSpawnBox(v)).ToArray();
        }
    }
}
