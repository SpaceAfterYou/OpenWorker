using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using Lua;
using Lua.Standard;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The spawn boxes of one group come out together when the script calls the group, and the script hears the group once
/// its last monster has fallen.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SpawnGroupTest
{
    private const int Group = 701;
    private const int Wolf = 1000;

    private static string Basic(int id) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="500.000,500.000,100.000" /> <m_vPosBottomRight type="7" value="600.000,600.000,200.000" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="100.000,100.000,100.000" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static string SpawnBox(int id, int sector, int group)
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(id)} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var monster = slot == 1 ? Wolf : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{monster}" /> <m_iChance{slot} type="0" value="{(monster == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Random" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="WaitSignal" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="0" /> <m_iMaxEntityCount type="0" value="1" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="3.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="1.0000" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="{group}" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    private const string Script = """
        Fallen = {}
        function OnRealDieEx(nGroupID, pMaze)
            Fallen[#Fallen + 1] = nGroupID
        end
        """;

    [TestMethod]
    public async Task TheScriptHearsAGroupOnceItsLastMonsterHasFallen()
    {
        var world = World.Create();

        try
        {
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });

            // Two boxes of the group in its sector, one of the group in another sector, and one box of no group.
            var map = "<root><Batchs eventtype=\"EventBox\">" + SpawnBox(11, 1, Group) + SpawnBox(12, 1, Group) + SpawnBox(13, 2, Group) +
                SpawnBox(14, 1, 0) + "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";
            var state = LuaState.Create();

            state.OpenStandardLibraries();

            var maze = TestMazes.Create(world, player, state: state, map: map);

            await state.DoStringAsync(Script);

            maze.ExecuteEventSpawnGroupLua(Group);
            maze.ExecuteEventSpawnLua(14);

            var monsters = maze.Creatures.FindMonsters((_, _) => true);

            Assert.HasCount(3, monsters, "the boxes of the group in its sector, and the other box");

            async Task FallAsync(Entity monster)
            {
                var box = world.Get<NonPlayableCreatureComponent>(monster).SpawnBox;

                maze.Creatures.Remove(monster);

                await state.OnDie(Wolf, box, maze);
            }

            var grouped = monsters.Where(e => world.Get<NonPlayableCreatureComponent>(e).SpawnBox != 14).ToArray();

            await FallAsync(grouped[0]);
            Assert.AreEqual(0, Fallen(state), "a monster of the group still stands");

            await FallAsync(maze.Creatures.FindMonsters((_, _) => true).Single(e => world.Get<NonPlayableCreatureComponent>(e).SpawnBox == 14));
            await FallAsync(grouped[1]);
            Assert.AreEqual(1, Fallen(state));

            maze.ExecuteEventSpawnGroupLua(Group);

            foreach (var monster in maze.Creatures.FindMonsters((_, _) => true))
            {
                await FallAsync(monster);
            }

            Assert.AreEqual(2, Fallen(state), "a group called out again falls again");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static int Fallen(LuaState state) => state.Environment["Fallen"].Read<LuaTable>().ArrayLength;
}
