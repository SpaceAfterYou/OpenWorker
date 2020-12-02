using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Character
{
    [Request]
    public readonly struct SelectRequest
    {
        public int Id { get; }
        private byte[] Unknown { get; }

        public SelectRequest(BinaryReader br)
        {
            Id = br.ReadInt32();
            Unknown = br.ReadBytes(13);
        }
    }
}
