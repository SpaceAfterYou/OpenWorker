using Microsoft.Extensions.Configuration;
using System.IO;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace Core.Systems.GameSystem
{
    public abstract class VData : VArchive
    {
        public VData(IConfiguration configuration, byte fileId) :
            base(Path.Join(configuration["Game:Dir"], "datas", $"data{fileId}.v"), configuration["Game:Datas:Passwords:Data49"])
        { }
    }

    public sealed class VData49 : VData
    {
        public VData49(IConfiguration configuration) : base(configuration, 49)
        {
        }
    }

    public sealed class VData12 : VData
    {
        public VData12(IConfiguration configuration) : base(configuration, 12)
        {
        }
    }
}