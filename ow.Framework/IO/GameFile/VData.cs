using Microsoft.Extensions.Configuration;
using System.IO;

namespace ow.Framework.IO.GameFile
{
    public abstract class VData : VArchive
    {
        public VData(IConfiguration configuration, byte fileId) :
            base(Path.Join(configuration["Game:Dir"], "datas", $"data{fileId}.v"), configuration[$"Game:Datas:Passwords:Data{fileId}"])
        { }
    }
}
