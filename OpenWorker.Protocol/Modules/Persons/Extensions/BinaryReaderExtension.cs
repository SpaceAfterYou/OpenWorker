using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.Modules.Persons.Extensions;

internal static class BinaryReaderExtension
{
    internal static string ReadPersonName(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(21);
    }
    
    internal static StuffLevel ReadStuffLevel(this BinaryReader reader)
    {
        return (StuffLevel)reader.ReadByte();
    }

    internal static FactionGroup ReadFactionGroup(this BinaryReader reader)
    {
        return (FactionGroup)reader.ReadByte();
    }
}