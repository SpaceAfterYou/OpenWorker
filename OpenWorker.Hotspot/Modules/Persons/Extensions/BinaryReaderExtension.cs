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
    
    internal static ReviveType ReadReviveType(this BinaryReader reader)
    {
        return (ReviveType)reader.ReadByte();
    }
    
    internal static PasswordCheckType ReadPasswordCheckType(this BinaryReader reader)
    {
        return (PasswordCheckType)reader.ReadByte();
    }
    
    internal static CommunityState ReadCommunityState(this BinaryReader reader)
    {
        return (CommunityState)reader.ReadByte();
    }
    
    internal static string ReadCommunityComment(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(PersonModuleDefines.MaxCommunityCommentLength);
    }
    
    internal static string ReadCommunityMemo(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(PersonModuleDefines.MaxCommunityMemoLength);
    }
    
    internal static ChangeServerType ReadChangeServerType(this BinaryReader reader)
    {
        return (ChangeServerType)reader.ReadByte();
    }
    
    internal static FactionGroup ReadFactionGroup(this BinaryReader reader)
    {
        return (FactionGroup)reader.ReadByte();
    }
}