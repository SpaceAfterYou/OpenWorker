using Microsoft.Extensions.Configuration;
using ow.Service.District.Network;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    internal class DimensionRepository : Dictionary<ushort, Dimension>
    {
        public DimensionRepository(IConfiguration configuration, Instance instance) : base(GetDimensions(configuration, instance))
        {
        }

        internal bool Join(Session session) => this.Any(dimension => dimension.Value.TryJoin(session));

        private static Dictionary<ushort, Dimension> GetDimensions(IConfiguration configuration, Instance instance) => Enumerable
            .Range(byte.Parse(configuration["Dimensions:Offset"]), byte.Parse(configuration["Dimensions:Count"]))
            .ToDictionary(k => (ushort)k, v => new Dimension((ushort)v, instance));
    }
}