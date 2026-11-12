using System.Text;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A banner of the cash shop. Its address goes as the strings of .NET's own writer do (the count of its UTF-8 bytes in
/// seven-bit groups, then the bytes), which no format of the generator is, so the writer is written by hand. The list is
/// always empty today; whether the client reads this layout is not checked.
/// </summary>
public readonly struct ShopBannerValue : IWireWritable
{
#region Message: Body

    public string Url { get; init; }
    public TimeSpan Time { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        var url = Url ?? string.Empty;

        // The length in groups of seven bits, the lowest first, the high bit set on every group but the last.
        var length = (uint)Encoding.UTF8.GetByteCount(url);

        while (length >= 0x80)
        {
            writer.WriteByte((byte)(length | 0x80));
            length >>= 7;
        }

        writer.WriteByte((byte)length);
        writer.WriteBytes(Encoding.UTF8.GetBytes(url));
        writer.WriteTimeInSeconds32(Time);
    }

#endregion Interface: IWireWritable
}
