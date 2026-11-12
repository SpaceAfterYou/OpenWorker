using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Modules.Persons.Extensions;

/// <summary>
/// The name of a person for the readers of <see cref="BinaryReader"/> the tests read letters back with.
/// </summary>
internal static class BinaryReaderExtension
{
    internal static string ReadPersonName(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(21);
    }
}
