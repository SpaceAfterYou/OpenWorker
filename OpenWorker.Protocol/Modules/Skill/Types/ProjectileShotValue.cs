using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct ProjectileShotValue
{
    public int Skill { get; init; }
    public short Attack { get; init; }
    public Vector3 Position { get; init; }
    public Vector3 Direction { get; init; }
    public Vector3 Right { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public short AdditionalDirectionX { get; init; }
    public short AdditionalDirectionY { get; init; }
    public uint Session { get; init; }
    public ActorValue Target { get; init; }
}
