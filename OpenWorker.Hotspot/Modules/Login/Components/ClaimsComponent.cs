using System.Runtime.InteropServices;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Hotspot.Modules.Login.Components;

[EntityComponent(EntityComponentService.All)]
[StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
public readonly record struct ClaimsComponent
{
    [field: FieldOffset(0)]
    public long Key { get; }

    [field: FieldOffset(0)]
    public int Account { get; }

    [field: FieldOffset(4)]
    public int Salt { get; }

    public ClaimsComponent(SessionValue claims) : this(claims.Key)
    {
        
    }

    public ClaimsComponent(int account, int salt)
    {
        Account = account;
        Salt = salt;
    }
    
    public ClaimsComponent(long key)
    {
        Key = key;
    }
    
    public static implicit operator int(ClaimsComponent component) => component.Account;
}