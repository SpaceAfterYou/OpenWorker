using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct ToggleWeaponRequest
    {
        public int CharacterId { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public uint Toggle { get; }
        public uint Unknown1 { get; }

        public ToggleWeaponRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            Toggle = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
        }
    }
}
