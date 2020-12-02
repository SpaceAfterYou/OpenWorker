using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests
{
    [Request]
    public readonly struct CityConnectRequest
    {
        public int CharacterId { get; }
        private byte[] Unknown { get; }

        public CityConnectRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Unknown = br.ReadBytes(13);
        }
    }
}
