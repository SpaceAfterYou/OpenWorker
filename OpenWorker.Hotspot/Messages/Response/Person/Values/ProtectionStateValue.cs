using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot.Modules.Login.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(short))]
public readonly struct ProtectionStateValue(Entity entity)
{
    [field: FieldOffset(0)]
    public short Value { get; } = 0;

    [field: FieldOffset(0)]
    public bool HasSecondPassword { get; } = entity.Get<SecondPasswordComponent>().Has;

    [field: FieldOffset(1)]
    public bool HasTradePassword { get; } = entity.Get<TradePasswordComponent>().Has;
}