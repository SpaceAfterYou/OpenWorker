using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Types;

namespace OpenWorker.Hotspot.Modules.Chat.Extensions;

internal static class BinaryReaderExtension
{
    public static ChatMessageAppearance ReadChatMessageAppearance(this BinaryReader reader)
    {
        return (ChatMessageAppearance)reader.ReadByte();
    }
    
    public static string ReadChatMessage(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(ChatModuleDefines.MaxMessageLength);
    }
}