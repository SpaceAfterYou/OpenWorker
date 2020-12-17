using Microsoft.Extensions.Configuration;
using ow.Framework.FS.Entities;
using ow.Framework.IO.Network;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    internal class DimensionRepository : Dictionary<ushort, DimensionEntity>
    {
        public DimensionRepository(IConfiguration configuration) : base(GetDimensions(configuration))
        {
        }

        internal bool Join(GameSession session) => this.Any(dimension => dimension.Value.TryJoin(session));

        private static Dictionary<ushort, DimensionEntity> GetDimensions(IConfiguration configuration) => Enumerable
            .Range(byte.Parse(configuration["Dimensions:Offset"]), byte.Parse(configuration["Dimensions:Count"]))
            .ToDictionary(k => (ushort)k, v => new DimensionEntity((ushort)v));
    }
}