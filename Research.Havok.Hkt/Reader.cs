// https://www.youtube.com/watch?v=7vWzIPUX5CQ&list=RD7vWzIPUX5CQ&start_radio=1
//

using System.Diagnostics;
using System.Text;
using OpenWorker.Havok.Enums;
using OpenWorker.Havok.Extensions;

namespace Research.Havok.Hkt;

internal static class Reader
{
    public static async ValueTask Read()
    {
        const string path = @"T:\Games\HanPurple\soulworker\datas\World\Navmesh\F031_ROCCOTOWN.hkt";

        var data = await File.ReadAllBytesAsync(path).ConfigureAwait(false);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var magic1 = (Magic)reader.ReadUInt32();
        var magic2 = (Magic)reader.ReadUInt32();

        Debug.Assert(magic1 == Magic.BinaryLeft);
        Debug.Assert(magic2 == Magic.BinaryRight);

        var remainClasses = new List<string>();
        var classes = new List<string>();

        do
        {
            var tagType = reader.ReadTagType();
            Debug.WriteLine(tagType);

            switch (tagType)
            {
                case TagType.FileInfo:
                    var version = reader.ReadHavokNumber();

                    switch (version)
                    {
                        // case 0:
                        //     break;

                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            var length = reader.ReadHavokNumber();
                            var buffer = reader.ReadBytes(length);
                            var value = Encoding.ASCII.GetString(buffer);
                            Debug.WriteLine(value);
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    break;

                case TagType.Eof:
                    Console.WriteLine("EOF");
                    return;

                case TagType.Metadata:
                    reader.ReadHavokClass(remainClasses, classes);
                    break;

                case TagType.Object:
                case TagType.ObjectRemember:
                case TagType.ObjectNull:
                    reader.ReadObjectTopLevel();
                    return;

                case TagType.None:
                    Console.WriteLine("None");
                    return;
                case TagType.FileEnd:
                    Console.WriteLine("FileEnd");
                    return;
                
                case TagType.ObjectBackref:
                default:
                    throw new NotImplementedException();
            }
        } while (true);

        Console.WriteLine("end");
    }
}