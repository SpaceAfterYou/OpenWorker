using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace ow.Framework.IO.File
{
    public abstract class VisionZipFile : ZipFile
    {
        protected VisionZipFile(string path, string password = null) : base(GetFile(path), false) => Password = password;

        private static MemoryStream GetFile(string path)
        {
            byte[] data = System.IO.File.ReadAllBytes(path);
            Exchange(ref data);

            return new(data);
        }

        private static void Exchange(ref byte[] raw)
        {
            for (int q = 0; q != raw.Length; ++q)
                raw[q] ^= 0x55;
        }
    }
}