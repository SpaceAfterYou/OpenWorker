using Ionic.Zip;
using System;
using System.IO;

namespace TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization
{
    public class VArchive : IDisposable
    {
        public ZipEntry this[string filename] => _zipEntries[filename];

        public VArchive(string path, string password = null)
        {
            byte[] data = File.ReadAllBytes(path);

            Exchange(ref data);

            _memoryStream = new(data);

            _zipEntries = ZipFile.Read(_memoryStream);
            _zipEntries.Password = password;
        }

        public void Dispose()
        {
            _zipEntries.Dispose();
            _memoryStream.Dispose();

            GC.SuppressFinalize(this);
        }

        private static void Exchange(ref byte[] raw)
        {
            for (int q = 0; q != raw.Length; ++q)
            {
                raw[q] ^= 0x55;
            }
        }

        private readonly ZipFile _zipEntries;
        private readonly MemoryStream _memoryStream;
    }
}