using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A player takes control of a monster, or lets it go. Every client makes the monster itself, with the actor and the row
/// given, and hides the player while it controls one; a table of zero lets the monster go.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MonsterControlResponse : IResponseHotspotMessage
{
    /// <summary>
    /// No animation of the player: the one a release carries.
    /// </summary>
    public const int NoAnimation = -1;

    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.ControlMonsterBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Owner { get; init; }

    /// <summary>
    /// The controlled monster; zero when it is let go.
    /// </summary>
    public ActorValue Monster { get; init; }

    public Vector3 Position { get; init; }

    /// <summary>
    /// The way the owner faces.
    /// </summary>
    public float Yaw { get; init; }

    /// <summary>
    /// The row of the table of monsters; zero lets the monster go.
    /// </summary>
    public int Table { get; init; }

    /// <summary>
    /// How visible the monster is as it comes in.
    /// </summary>
    public float Alpha { get; init; }

    /// <summary>
    /// How long the monster takes to come in, in seconds.
    /// </summary>
    public float Blend { get; init; }

    /// <summary>
    /// The animation the monster comes in with, by its number in the owner's action file.
    /// </summary>
    public int Animation { get; init; }

    /// <summary>
    /// Whether the monster goes where the owner goes.
    /// </summary>
    public bool Follows { get; init; }
}
