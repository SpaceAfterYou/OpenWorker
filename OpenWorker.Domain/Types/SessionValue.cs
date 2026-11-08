using System.Runtime.InteropServices;
using System.Security.Cryptography;
using OpenWorker.Wire;

namespace OpenWorker.Domain.Types;

[StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
public readonly struct SessionValue : IWireReadable<SessionValue>, IWireWritable
{
    [field: FieldOffset(0)]
    public long Key { get; }

    [field: FieldOffset(0)]
    public int Account { get; }

    [field: FieldOffset(4)]
    public int Salt { get; }

    public SessionValue(int account)
    {
        Account = account;
        Salt = RandomNumberGenerator.GetInt32(int.MaxValue);
    }

    public SessionValue(int account, int salt)
    {
        Account = account;
        Salt = salt;
    }

    public SessionValue(long key)
    {
        Key = key;
    }

    public SessionValue(BinaryReader reader)
    {
        Key = reader.ReadInt64();
    }

    /// <summary>
    /// The key on the wire, 64 bits.
    /// </summary>
    public static SessionValue Read(ref PacketReader reader) => new(reader.ReadInt64());

    public void Write(ref PacketWriter writer) => writer.WriteInt64(Key);
}