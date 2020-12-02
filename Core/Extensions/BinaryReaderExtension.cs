using System.IO;
using System.Numerics;

namespace Core.Extensions
{
    public static class BinaryReaderExtension
    {
        public static Vector3 ReadVector3(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        public static Vector2 ReadVector2(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle());
    }
}