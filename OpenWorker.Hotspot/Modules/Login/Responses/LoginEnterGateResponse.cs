using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Components;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command)]
public readonly struct LoginEnterGateResponse(Entity entity, bool hasError = false) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.EnterServerRes;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(hasError);
        writer.Write(hasError ? -1 : entity.Get<ClaimsComponent>().Account);
    }

    public static LoginEnterGateResponse Error => new();
}