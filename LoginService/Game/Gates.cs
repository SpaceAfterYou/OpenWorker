using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace LoginService.Game
{
    public sealed class Gates : List<Gate>
    {
        public Gates(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        public static IEnumerable<Gate> GetGates(IConfiguration configuration) =>
            configuration.GetSection("Gates").GetChildren().AsEnumerable().Select(c => new Gate(c));
    }
}
