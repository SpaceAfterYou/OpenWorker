using OpenWorker.Hotspot.Modules.League.DataTypes;

namespace OpenWorker.Hotspot.Modules.League.Extensions;

/// <summary>
/// The values of a league for the answers that still write through a <see cref="BinaryWriter"/>: those whose time to
/// wait goes only with some results.
/// </summary>
public static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, LeagueApplicant value)
    {
        value.Write(writer);
    }

    internal static void Write(this BinaryWriter writer, LeagueBoardEntry value)
    {
        value.Write(writer);
    }
}
