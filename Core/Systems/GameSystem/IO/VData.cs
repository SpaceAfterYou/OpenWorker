using Microsoft.Extensions.Configuration;
using System.IO;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace Core.Systems.GameSystem.IO
{
    public abstract class VData : VArchive
    {
        public VData(IConfiguration configuration, byte fileId) :
            base(Path.Join(configuration["Game:Dir"], "datas", $"data{fileId}.v"), configuration["Game:Datas:Passwords:Data49"])
        { }
    }
}