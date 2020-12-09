using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Party
{
    [Request]
    public readonly struct ChangeMasterRequest
    {
        public int Id { get; }

        public ChangeMasterRequest(BinaryReader br)
            => Id = br.ReadInt32();
    }
}
