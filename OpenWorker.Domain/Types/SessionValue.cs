using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenWorker.Domain.Types;

[StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
public readonly struct SessionValue
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
}