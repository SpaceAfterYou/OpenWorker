using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game
{
    internal class Dimensions : Dictionary<ushort, DimensionEntity>
    {
        public Dimensions(IConfiguration configuration) : base(GetDimensions(configuration))
        {
        }

        private static Dictionary<ushort, DimensionEntity> GetDimensions(IConfiguration configuration) => Enumerable
            .Range(byte.Parse(configuration["Dimensions:Offset"]), byte.Parse(configuration["Dimensions:Count"]))
            .ToDictionary(k => (ushort)k, v => new DimensionEntity((ushort)v));
    }
}