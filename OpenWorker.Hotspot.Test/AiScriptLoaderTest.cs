using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class AiScriptLoaderTest
{
    private static readonly string? Folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");

    private sealed class Recorder : ILogger<AiScriptLoader>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception) + " " + exception?.Message);
        }
    }

    private static AiScriptLoader Loader() => new(NullLogger<AiScriptLoader>.Instance);

    private static AiProfile Parse(string script)
    {
        return Loader().Parse("test", Encoding.UTF8.GetBytes(script));
    }

    [TestMethod]
    public void CallsOfAScriptEndUpInTheProfile()
    {
        var profile = Parse("""
            function Initial_Normal(self, monster)
                local ai = monster:GetAi()
                ai:SetAiCheckTime( 0.1 )
                ai:SetFindEnemyInfo( 1, 0.5, 1500.0 )
                ai:SetReturnDistance( 5000 )
                ai:SetSkillGroupInfo( 0, 0, 1, 2, 3 )
                ai:SetCommonAction( 0, "B_Idle_01" )
                ai:RegisterStateFunctions( Soulworker.FSMSTATES_COMMON_ACTION1 )
                ai:RegisterStateVars( Soulworker.FSMSTATES_SELECT_ACTION, Soulworker.STATEVAR_LIFE_TIME, 600, 800 )
                ai:RegisterStateVars( Soulworker.FSMSTATES_SELECT_ACTION, Soulworker.STATEVAR_NEXT_STATE, Soulworker.FSMSTATES_ATTACK, 100 )
                ai:RegisterConditions( Soulworker.FSMSTATES_ATTACK, Soulworker.FSMSTATES_ATTACK_SKILL1, Soulworker.FSMVAR_TARGETDISTANCE, "<", 400 )
                ai:RegisterConditions( Soulworker.FSMSTATES_ATTACK, Soulworker.FSMSTATES_ATTACK_SKILL1, Soulworker.FSMVAR_RANDOM, "<", 8000 )
                ai:RegisterSkillConditions( 1, Soulworker.FSMVAR_RANDOM, "<", 4000, 0 )
            end
            """);

        Assert.AreEqual(0.1f, profile.CheckTime);
        Assert.IsTrue(profile.SeeksEnemies);
        Assert.AreEqual(1500f, profile.SightDistance);
        Assert.AreEqual(5000f, profile.ReturnDistance);
        Assert.AreEqual("B_Idle_01", profile.CommonActions[0]);
        Assert.Contains(AiState.CommonAction1, profile.States);
        Assert.Contains(AiState.Attack, profile.States);

        var settings = profile.GetSettings(AiState.SelectAction);

        Assert.AreEqual(600, settings.LifeMin);
        Assert.AreEqual(800, settings.LifeMax);
        Assert.AreEqual((AiState.Attack, 100), settings.NextStates.Single());

        var transition = profile.GetTransitions(AiState.Attack).Single();

        Assert.AreEqual(AiState.AttackSkill1, transition.Output);
        Assert.HasCount(2, transition.Conditions);
        Assert.AreEqual(AiVariable.TargetDistance, transition.Conditions[0].Variable);
        Assert.HasCount(1, profile.SkillRules[0].Conditions);
    }

    [TestMethod]
    public void AnUnknownNameInAScriptIsLeftAlone()
    {
        var profile = Parse("""
            function Initial_Normal(self, monster)
                local ai = monster:GetAi()
                ai:SetGaze( 2.5, 150, 170 )
                ai:RegisterConditions( Soulworker.FSMSTATES_FIND_ENEMY, Soulworker.FSMSTATES_ATTACK, Soulworker.FSMVAR_RANDOM, "<", 5000 )
                ai:SetAiCheckTime( 0.2 )
            end
            """);

        Assert.AreEqual(0.2f, profile.CheckTime);
        Assert.IsEmpty(profile.Transitions);
    }

    [TestMethod]
    public void AScriptWithoutAnEntryPointMakesAnEmptyProfile()
    {
        var profile = Parse("-- nothing here");

        Assert.IsEmpty(profile.States);
    }

    [TestMethod]
    public void TheMarkOfTheEncodingAndOtherBytesInCommentsAreNoProblem()
    {
        var script = new List<byte>([0xEF, 0xBB, 0xBF]);

        script.AddRange("-- "u8.ToArray());
        script.AddRange([0xB1, 0xE2, 0xBA, 0xBB]);
        script.AddRange("\nfunction Initial_Normal(self, monster) monster:GetAi():SetAiCheckTime(0.3) end\n"u8.ToArray());

        var profile = Loader().Parse("test", script.ToArray());

        Assert.AreEqual(0.3f, profile.CheckTime);
    }

    [TestMethod]
    public void AMissingScriptMeansAMonsterThatDoesNotThink()
    {
        var profile = new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, Path.Combine(Path.GetTempPath(), "no-such-folder")).Get("Ai_Nothing");

        Assert.AreSame(AiProfile.Empty, profile);
    }

    [TestMethod]
    public void EveryScriptOfTheClientIsRead()
    {
        if (Folder is null || !Directory.Exists(Folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
        }

        var recorder = new Recorder();
        var loader = new AiScriptLoader(recorder, Folder);
        var files = Directory.GetFiles(Folder, "*.lua");
        var thinking = 0;
        var empty = new List<string>();

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var profile = loader.Get(name);

            // One dummy of the client has a stray closing bracket that no Lua accepts.
            if (name == "Ai_MOB_0000_Dummy_02")
            {
                Assert.AreSame(AiProfile.Empty, profile);
                recorder.Messages.Clear();

                continue;
            }

            Assert.IsEmpty(recorder.Messages, name + ": " + string.Join("; ", recorder.Messages));

            if (profile.States.Count > 0)
            {
                thinking++;
            }
            else
            {
                empty.Add(name);
            }
        }

        // Dummies and objects have no states, everything else must think.
        Assert.IsGreaterThan(files.Length * 9 / 10, thinking, string.Join(", ", empty.Take(20)));
    }

    [TestMethod]
    public void AMonsterOfTheClientFightsAsItsScriptSays()
    {
        if (Folder is null || !File.Exists(Path.Combine(Folder, "Ai_MOB_0021_Nastypuppet.lua")))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
        }

        var profile = new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, Folder).Get("ai_mob_0021_nastypuppet");

        Assert.AreEqual(0.1f, profile.CheckTime);
        Assert.IsTrue(profile.SeeksEnemies);
        Assert.AreEqual(1000f, profile.SightDistance);
        Assert.AreEqual(5000f, profile.ReturnDistance);
        Assert.IsTrue(profile.HasSkillGroups);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, profile.SkillGroups[0].ToArray());
        CollectionAssert.AreEqual(new[] { 4, 3, 0 }, profile.SkillGroups[1].ToArray());
        Assert.Contains(AiState.Attack, profile.States);
        Assert.IsNotEmpty(profile.GetTransitions(AiState.Attack));
        Assert.AreEqual(4000, profile.GetSettings(AiState.Wait).LifeMin);
    }
}
