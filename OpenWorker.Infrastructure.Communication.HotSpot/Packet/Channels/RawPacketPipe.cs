using System.Buffers;
using System.IO.Pipelines;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels;

internal sealed record RawPacketPipe : IReadOnlyRawPacketPipe, IWriteOnlyRawPacketPipe, IAsyncDisposable
{
    public void Write(ReadOnlySpan<byte> _raw) => _pipe.Writer.Write(_raw); 

    public Stream OpenStream() => _pipe.Reader.AsStream();

    public async ValueTask DisposeAsync()
    {
        await _pipe.Writer.CompleteAsync();
        await _pipe.Reader.CompleteAsync();
    }

    private readonly Pipe _pipe = new();
}
