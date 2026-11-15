using System.Collections.Frozen;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// The numbers the client gives the animations of a hero's file, by which it names the animation of a combo it plays (06/4A).
/// The client numbers the moves, falls and stands it knows by name itself; every other animation of the file takes, in the
/// order of the file, the next motion class from <see cref="FirstMotion"/> on, and its number is the class times a thousand
/// plus the hundred of the battle pose.
/// </summary>
public static class AnimationNumbers
{
    public const int FirstMotion = 45;

    private const int PerMotion = 1000;
    private const int BattlePose = 100;

    /// <summary>
    /// Animations the client numbers itself, so they take no class of their own here.
    /// </summary>
    private static readonly FrozenSet<string> NamedByClient = new[]
    {
        "B_DMG_L", "B_DMG_R", "B_Dash_End", "B_Dash_Jump_End", "B_Dash_Jump_Loop", "B_Dash_Jump_Start", "B_Dash_Loop",
        "B_Dash_Start", "B_Death_Down", "B_Death_Down_Wait", "B_Death_Stand", "B_Evade_B", "B_Evade_F", "B_Evade_L",
        "B_Evade_R", "B_Idle_01", "B_Interaction_Object_End", "B_Interaction_Object_Loop", "B_Interaction_Object_Start",
        "B_Jump_Air_Hit", "B_Jump_Land_C", "B_Jump_Loop_B", "B_Jump_Loop_F", "B_Jump_Loop_L", "B_Jump_Loop_R",
        "B_Jump_Start_B", "B_Jump_Start_F", "B_Jump_Start_L", "B_Jump_Start_R", "B_KB_End", "B_KB_Start", "B_KD_Ham_B_Air_Hit",
        "B_KD_Ham_B_Down", "B_KD_Ham_B_Down_Hit", "B_KD_Ham_B_End", "B_KD_Ham_B_Loop", "B_KD_Ham_B_Raise", "B_KD_Ham_B_Start",
        "B_KD_Ham_F_Air_Hit", "B_KD_Ham_F_Down", "B_KD_Ham_F_Down_Hit", "B_KD_Ham_F_End", "B_KD_Ham_F_Loop", "B_KD_Ham_F_Raise",
        "B_KD_Ham_F_Start", "B_KD_Str_Air_Hit", "B_KD_Str_Down", "B_KD_Str_Down_Hit", "B_KD_Str_End", "B_KD_Str_Loop",
        "B_KD_Str_Raise", "B_KD_Str_Start", "B_KD_Upp_Air_Hit", "B_KD_Upp_Down", "B_KD_Upp_Down_Hit", "B_KD_Upp_End",
        "B_KD_Upp_Loop", "B_KD_Upp_Raise", "B_KD_Upp_Start", "B_Looting", "B_N_Mode", "B_Roll_B", "B_Roll_F", "B_Roll_L",
        "B_Roll_R", "B_Run_B", "B_Run_F", "B_Run_F_End", "B_Run_L", "B_Run_R", "B_Stand", "B_Stun", "B_Win", "N_B_Mode",
        "N_Dash_End", "N_Dash_Jump_End", "N_Dash_Jump_Loop", "N_Dash_Jump_Start", "N_Dash_Loop", "N_Dash_Start",
        "N_Jump_Land_C", "N_Jump_Loop_F", "N_Jump_Start_F", "N_Run_F", "N_Run_F_End", "N_Stand"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Entries of a file that are no animations a character plays.
    /// </summary>
    private static readonly FrozenSet<string> NotPlayed = new[] { "AnimationOfAll", "Destruction" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// The animations of the file by the numbers the client gives them; the ones it numbers itself are not in it.
    /// </summary>
    public static IReadOnlyDictionary<int, ActionAnimation> Of(ActionFile file)
    {
        var numbers = new Dictionary<int, ActionAnimation>();
        var motion = FirstMotion;

        foreach (var (name, _) in file.Indexes.OrderBy(e => e.Value))
        {
            if (NamedByClient.Contains(name) || NotPlayed.Contains(name) || !file.Animations.TryGetValue(name, out var animation))
            {
                continue;
            }

            numbers[motion++ * PerMotion + BattlePose] = animation;
        }

        return numbers;
    }
}
