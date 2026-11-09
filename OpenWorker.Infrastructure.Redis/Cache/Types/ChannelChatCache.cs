using OpenWorker.Hotspot.Modules.Chat.Enums;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class ChannelChatCache
{
    [RedisIdField]
    public Guid Guid { get; init; } = Guid.CreateVersion7();
    
    [Indexed]
    public required short Channel { get; set; }
    
    [RedisIdField]
    public required string Message { get; set; }
    
    [RedisIdField]
    public required ChatMessageAppearance Appearance { get; set; }
    
    [RedisIdField]
    public required int Actor { get; set; }
}