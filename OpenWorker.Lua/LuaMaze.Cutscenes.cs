using Arch.Core;
using Lua;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    private const byte CutsceneStopped = 0;
    private const byte CutscenePlaying = 2;

    /// <summary>
    /// The cutscene the player is watching, if any.
    /// </summary>
    private string? Cutscene { get; set; }

    /// <summary>
    /// A cutscene starts or is skipped on the client. The maze waits for the boss while one plays, and its script
    /// hears when it is over.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask UpdateCutsceneAsync(Entity player, bool play, string name)
    {
        if (!Started || Find(player) is null || !TrySession(player, out var session))
        {
            return;
        }

        if (play)
        {
            Cutscene = name;

            SetMazeState(MazeState.StartBoss);

            session.Send(new PersonPlayCutsceneResponse { State = CutscenePlaying });

            return;
        }

        session.Send(new PersonPlayCutsceneResponse { State = CutsceneStopped });

        if (Cutscene is null)
        {
            return;
        }

        Cutscene = null;

        SetMazeState(MazeState.WaitToDie);

        await State.OnCutsceneOff(name, this);
    }
}
