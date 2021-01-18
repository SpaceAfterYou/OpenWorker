using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace SoulCore.Game.Datas.World.Navmesh
{
    /// <summary>
    /// Magic marker constants used in the file.
    /// </summary>
    internal enum TagType
    {
        /// <summary>
        /// End of file.
        /// </summary>
        Eof = -1,

        /// <summary>
        /// Invalid tag.
        /// </summary>
        None = 0,

        /// <summary>
        /// File header info. Followed by an integer version number.
        /// The rest of the header data is determined by the version number.
        /// </summary>
        FileInfo = 1,

        /// <summary>
        /// The following item is an hkGenericClass
        /// </summary>
        MetaData = 2,

        /// <summary>
        /// The following item is an hkGenericObject which will not be referenced again.
        /// </summary>
        Object = 3,

        /// <summary>
        /// The following item is an object which may be referenced again from TAG_OBJECT_BACKREF. The id is implicitly the count of preceding remembered objects.
        /// </summary>
        ObjectRemember = 4,

        /// <summary>
        /// Refer to a previously encountered object.
        /// Followed by the integer object index.
        /// </summary>
        ObjectBackRef = 5,

        /// <summary>
        /// The null object pointer, only used in version 0 and 1
        /// </summary>
        ObjectNull = 6,

        /// <summary>
        /// End of file marker
        /// </summary>
        fileEnd = 7,
    }

    internal enum Magic : uint
    {
        First = 0xCAB00D1E,
        Second = 0xD011FACE,
    };

    internal sealed record Header
    {
        internal Magic Magic1;
        internal Magic Magic2;
        internal byte FileInfo;
        internal ushort Version;
        internal string SDK;

        internal Header(BinaryReader br)
        {
            Magic1 = (Magic)br.ReadUInt32();
            Magic2 = (Magic)br.ReadUInt32();
            FileInfo = br.ReadByte();
            Version = br.ReadUInt16();
            SDK = br.ReadString();
        }
    };

    [Serializable]
    public sealed class DataObject : ISerializable
    {
        public void readObjectTopLevel(BinaryReader br)
        { }

        public void readObject(BinaryReader br)
        { }

        public void Read(string path)
        {
            using FileStream fs = File.OpenRead(path);
            using BinaryReader br = new(fs);

            uint magicLeft = br.ReadUInt32();
            uint magicRight = br.ReadUInt32();
            Debug.Assert(!(magicLeft != (uint)Magic.First || magicRight != (uint)Magic.Second));

            byte u1 = br.ReadByte();

            int v32 = 0;
            do
            {
                v32 = br.ReadByte();
            } while ((v32 & 0x80u) != 0);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
