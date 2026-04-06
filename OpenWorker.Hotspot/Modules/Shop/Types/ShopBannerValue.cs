using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

public readonly struct ShopBannerValue(BinaryReader reader) : IWritableData
{
#region Message: Body
    
    public string Url { get; init; } = reader.ReadUtf8UnicodeString(498);
    public TimeSpan Time { get; init; } = reader.ReadTimeInSeconds32();

#endregion Message: Body
    
#region Interface: IWritableData
    
    public void Write(BinaryWriter writer)
    {
        writer.Write(Url);
        writer.Write(Time);
    }
    
#endregion Interface: IWritableData
}